using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Ant0nRocket.Lib.Dodb.DbContexts;
using Ant0nRocket.Lib.Dodb.Dto;
using Ant0nRocket.Lib.Dodb.Dto.Payloads.Abstractions;
using Ant0nRocket.Lib.Dodb.Extensions;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Helpers;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Models;
using Ant0nRocket.Lib.Std20.Cryptography;
using Ant0nRocket.Lib.Std20.Extensions;
using Ant0nRocket.Lib.Std20.IO;
using Ant0nRocket.Lib.Std20.Logging;
using Ant0nRocket.Lib.Std20.Reflection;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb
{
    /// <summary>
    /// A gateway for document-oriented database.<br />
    /// Before using this in your project execute:
    /// <code>
    ///     DodbGateway.RegisterDbContextGetterFunc([...function that will return DbContext...]);
    ///     DodbGateway.RegisterKnownPayloadTypes();
    ///     DodbGateway.RegisterDtoPayloadHandler([...Method that will handle your Dto objects...]);
    /// </code>
    /// </summary>
    public static class Dodb
    {
        #region Private fields

        private static readonly Logger logger = Logger.Create(nameof(Dodb));

        private static bool _isInitialized = false;

        private static Mutex _mutex = new();

        private static readonly Dictionary<string, int> _payloadType_Id_Cache = new();

        #endregion

        #region Constants

        private const string ERROR_GETTING_DBCONTEXT = $"Can't create DbContext. Check {nameof(Initialize)} were called with non-null args.";
        private const string ERROR_GETTING_PASSWORD_HASHER = $"Can't get password hasher. Check {nameof(Initialize)} were called with non-null args";
        private const string ERROR_NOT_INITIALIZED = $"Call {nameof(Initialize)} before using {nameof(Dodb)}";
        private const string ERROR_IPAYLOADS_REG_FAILED = "Failed to register IPayload classes";

        #endregion

        #region DbContext handler

        /*
         Why we need a DbContext getter?
         This class doesn't know about the final class you will work with but it has to be DodbContextBase.
         As you DbContext will be somewhere in external library we need to know how to get it.
         */

        private static GetDbContextHandler? _getDbContextHandler = null;

        /// <summary>
        /// Returnes a DbContext using <see cref="GetDbContextHandler"/> that was registered with
        /// <see cref="Initialize(GetDbContextHandler, DtoPayloadHandler)"/>.
        /// </summary>
        internal static DodbContextBase GetDbContext() =>
            _getDbContextHandler?.Invoke() ?? throw new ApplicationException(ERROR_GETTING_DBCONTEXT);

        #endregion

        #region DTO payload handler

        private static DtoPayloadHandler? _dtoPayloadHandler = null;

        #endregion

        #region Password hash handler

        private static GetPasswordHashHandler _getPasswordHashHandler = DefaultPasswordHashHandler;

        private static string DefaultPasswordHashHandler(string plainPassword)
        {
            return Hasher.ComputeHash(plainPassword).ToHexString();
        }

        internal static GetPasswordHashHandler GetPasswordHashHandler() =>
            _getPasswordHashHandler ?? throw new ApplicationException(ERROR_GETTING_PASSWORD_HASHER);

        #endregion

        #region Initialization        

        /// <summary>
        /// Performes initialization of a library.
        /// </summary>
        public static void Initialize(
            GetDbContextHandler getDbContextHandler,
            DtoPayloadHandler dtoPayloadHandler)
        {
            if (_isInitialized) return;

            _getDbContextHandler = getDbContextHandler ?? throw new NullReferenceException(nameof(getDbContextHandler));
            _dtoPayloadHandler = dtoPayloadHandler ?? throw new NullReferenceException(nameof(dtoPayloadHandler));

            // Important to set this flag here! TouchUser will throw if false.
            _isInitialized = true;
        }

        #endregion

        #region DTO handling

        private static IGatewayResponse? TryHandleDtoPayloadInternally(object dtoPayload, DodbContextBase dbContext)
        {
            var c = dbContext;
            return dtoPayload switch
            {
                _ => null
            };
        }

        private static IGatewayResponse? TryHandleDtoPayloadExternally(object dtoPayload, DodbContextBase dbContext)
        {
            if (_dtoPayloadHandler != null)
                return _dtoPayloadHandler(dtoPayload, dbContext);
            return null;
        }

        /// <summary>
        /// Function only tryes to apply <paramref name="dto"/> inside <paramref name="dbContext"/>.<br />
        /// It doesn't check trnsactions, doesn't valid, only handling!
        /// </summary>
        private static IGatewayResponse PushDtoObject(DtoBase dto, DodbContextBase dbContext)
        {
            var dtoType = dto.GetType();
            var dtoPayloadPropertyInfo = dtoType.GetProperties().FirstOrDefault(p => p.Name == "Payload") ??
                throw new ArgumentException("DTO doesn't have a Payload property");
            var dtoPayload = dtoPayloadPropertyInfo.GetValue(dto) ??
                throw new InvalidDataException("Can't get a value of Dto.Payload");

            var dtoHandleResponse = // first, try internal handler, then try external handler, or drop no handler found
                TryHandleDtoPayloadInternally(dtoPayload, dbContext) ??
                TryHandleDtoPayloadExternally(dtoPayload, dbContext) ??
                new GrDtoPayloadHandlerNotFound();

            var documentPayload = new DocumentPayload
            {
                DocumentRefId = dto.Id,
                PayloadTypeName = dtoPayload.GetType().FullName,
                PayloadJson = dtoPayload.AsJson(pretty: false),
            };

            var document = new Document
            {
                Id = dto.Id,
                UserId = dto.UserId,
                RequiredDocumentId = dto.RequiredDocumentId,
                DateCreatedUtc = dto.DateCreatedUtc,
                DocumentPayloadId = documentPayload.Id,
            };

            dbContext.Documents.Add(document);
            dbContext.DocumentPayloads.Add(documentPayload);
            return dtoHandleResponse;
        }

        /// <summary>
        /// 1. Throws <see cref="ApplicationException"/> if library wasn't initialized (see <see cref="Initialize(GetDbContextHandler, DtoPayloadHandler)"/>.<br />
        /// 2. Returnes <see cref="GrDtoValidationFailed"/> if there are some validation errors in DTO or its payload.<br />
        /// 4. Returnes <see cref="GrDtoDocumentExists"/> if any document with <paramref name="dto"/>.Id already exists.<br />
        /// 5. Returnes <see cref="GrDtoRequiredDocumentNotFound"/> if there is some document required to exist but not found.<br />
        /// 6. Returnes <see cref="GrDtoPayloadHandlerNotFound"/> if there is no handler found for payload.<br />
        /// 7. Returnes <see cref="GrDtoPushFailed"/> if there some errors durring commit.<br />
        /// <br />
        /// Othervise returnes some <see cref="IGatewayResponse"/><br />
        /// ------------------------<br />
        /// If <paramref name="externalDbContext"/> passed then all transaction control, saving, disposing - is not 
        /// a business of current function. If you need just push DTO and commit it - dont set <paramref name="externalDbContext"/>!
        /// </summary>
        public static IGatewayResponse PushDto(
            DtoBase dto,
            DodbContextBase? externalDbContext = default,
            Func<DtoBase, DodbContextBase, bool>? onDatabaseValidation = null,
            Action<DtoBase, DodbContextBase>? beforeCommit = null)
        {
            if (!_isInitialized) throw new ApplicationException(ERROR_NOT_INITIALIZED);

            _mutex.WaitOne(); // thread will stop here if mutex is busy

            #region Basic validation (will check properties according to annotations)

            var validator = new DtoValidator(dto).Validate();
            if (validator.ValidationResults.Count > 0)
            {
                logger.LogError($"Invalid DTO '{dto.Id}': {string.Join(", ", validator.ErrorsList)}");
                return new GrDtoValidationFailed(validator.ErrorsList);
            }

            #endregion

            // ... ok, basic validation passed, let's go to database and check few more thing.
            // It's time to create a DbContext here.
            var dbContext = externalDbContext ?? GetDbContext(); // COULD BE EXTERNAL !!!

            using var transaction = externalDbContext == default ?
                dbContext.Database.BeginTransaction() : // our context - our transaction
                null; // external context - no transactions required!

            #region Database validations

            if (dbContext.Documents.Any(d => d.Id == dto.Id))
            {
                logger.LogWarning($"Can't apply DTO '{dto.Id}': document with this Id already exists");
                return new GrDtoDocumentExists { DocumentId = dto.Id };
            }

            if (dto.RequiredDocumentId != null)
            {
                if (!dbContext.Documents.Any(d => d.Id == dto.RequiredDocumentId))
                {
                    logger.LogWarning($"Can't apply DTO '{dto.Id}': required document '{dto.RequiredDocumentId}' doesn't exists");
                    return new GrDtoRequiredDocumentNotFound { RequesterId = dto.Id, RequiredDocumentId = dto.RequiredDocumentId };
                }
            }
            else // RequiredDocumentId is NOT specified
            {
                // This situation could be ONLY when there is a first document. 
                // So, if any document exists there should not be a DTO without RequiredDocumentId
                if (dbContext.Documents.Any())
                {
                    var message = $"DTO '{dto.Id}' must have RequiredDocumentId";
                    logger.LogError(message);
                    return new GrDtoPushFailed { Message = message };
                }
            }

            var externalDatabaseValidationResult = onDatabaseValidation?.Invoke(dto, dbContext) ?? true;
            if (externalDatabaseValidationResult == false)
            {
                var message = $"DTO '{dto.Id}' didn't pass database validation";
                logger.LogError(message);
                return new GrDtoValidationFailed(message);
            }

            #endregion

            // Alright! All checks done, DTO is ready to be applyied. But what about transaction?
            // If context is not external - let's start a transaction...
            //using var transaction = externalDbContext == default ? dbContext.Database.BeginTransaction() : null;

            // ... and when transaction starter (or not :)) - push dto deeper.
            var pushResult = PushDtoObject(dto, dbContext);

            if (pushResult.IsSuccess() == false)
            {
                logger.LogError($"Got {pushResult.GetType().Name} for DTO '{dto.Id}': {pushResult.AsJson()}");
                return pushResult;
            }

            #region If this function created dbContext then we save and commit...

            if (externalDbContext == default)
            {
                // What is going on here?
                // Very simple! If 'externalDbContext' is null means that we have create our dbContext
                // here (in this function). If so - we have a right (duty? :)) to save changes and
                // dispose what we have done.

                try
                {
                    beforeCommit?.Invoke(dto, dbContext);
                    dbContext.SaveChanges();
                    transaction?.Commit();
                }
                catch (Exception ex)
                {
                    var message = $"{ex.Message} " + ex.InnerException?.Message ?? string.Empty;
                    pushResult = new GrDtoPushFailed { Message = message };
                    logger.LogException(ex, $"DTO '{dto.Id}'");
                }
                finally
                {
                    dbContext.Dispose();
                }

                // ... but If 'externalDbContext' is NOT null then we must not do anything else, let
                // external owner of the context performs saving, disposing, etc.
            }

            #endregion

            _mutex.ReleaseMutex(); // other threads could work now

            return pushResult;
        }

        /// <inheritdoc cref="PushDto(DtoBase, DodbContextBase?, Func{DtoBase, DodbContextBase, bool}?, Action{DtoBase, DodbContextBase}?)"/>
        public static async Task<IGatewayResponse> PushDtoAsync(
            DtoBase dto,
            DodbContextBase? externalDbContext = default,
            Func<DtoBase, DodbContextBase, bool>? onDatabaseValidation = null,
            Action<DtoBase, DodbContextBase>? beforeCommit = null) => 
            await Task.Run(() => PushDto(dto, externalDbContext, onDatabaseValidation, beforeCommit));
        

        #endregion

        #region Public helper functions

        /// <summary>
        /// Function will create a DTO container for <typeparamref name="T"/> with
        /// filled <see cref="DtoBase.RequiredDocumentId"/>.<br />
        /// If <paramref name="userId"/> specified - then <see cref="DtoBase.UserId"/> will be filled.
        /// <b>N.B.!</b> Only <see cref="IPayload"/> classes valid for <typeparamref name="T"/>.
        /// </summary>
        public static DtoOf<T> CreateDto<T>(Guid? userId = default) where T : class, new()
        {
            if (!_isInitialized) throw new ApplicationException(ERROR_NOT_INITIALIZED);

            var result = new DtoOf<T>(userId);

            using var dbContext = GetDbContext();
            var latestDocumentId = dbContext
                .Documents
                .OrderByDescending(d => d.DateCreatedUtc)
                .Select(d => d.Id)
                .FirstOrDefault();

            if (latestDocumentId != default)
                result.RequiredDocumentId = latestDocumentId;

            return result;
        }



        #region Sync documents

        /// <inheritdoc cref="SyncDocuments(string)"/>
        public static async Task SyncDocumentsAsync(string syncDocumentsDirectoryPath) =>
            await Task.Run(() => SyncDocuments(syncDocumentsDirectoryPath));

        /// <summary>
        /// Performes syncthronization of known Documents inside <paramref name="syncDocumentsDirectoryPath"/>.<br />
        /// </summary>
        public static void SyncDocuments(string syncDocumentsDirectoryPath)
        {
            if (syncDocumentsDirectoryPath == default)
            {
                const string ERROR_MESSAGE = "SyncDirectoryPath is not set";
                logger.LogError(ERROR_MESSAGE);
            }
            else
            {
                FileSystemUtils.TouchDirectory(syncDocumentsDirectoryPath); // just to sure
                PerformSyncDocumentsIteration(syncDocumentsDirectoryPath);
            }
        }

        private static void PerformSyncDocumentsIteration(string syncDocumentsDirectoryPath)
        {
            var exportFromDate = ScanSyncDocumentsDirectoryMinExportDate(syncDocumentsDirectoryPath);
            var knownDocumentIds = GetKnownDocumentIds(exportFromDate);
            var exportedDocumentsIdAndPathDict = GetExportedDocumentsIdAndPathDict(syncDocumentsDirectoryPath);

            var documentIdsToExportList = GetDocumentIdsToExportList(knownDocumentIds, exportedDocumentsIdAndPathDict);

            var documentIdAndPathToImportDict = GetDocumentIdAndPathToImportDict(
                knownDocumentIds,
                exportedDocumentsIdAndPathDict);

            ExportDocuments(documentIdsToExportList, syncDocumentsDirectoryPath);
            ImportDocuments(documentIdAndPathToImportDict);
        }

        /// <summary>
        /// Sync directory could contain archives (zip or 7z) in format 'yyyyMMdd.(zip|7z)'.<br />
        /// This function will find them all and return latest covered date.<br />
        /// <b>NB! Function will not open that archive! It's a user duty to pack archives with care!</b>
        /// <br /><br />
        /// Assume, that we have folder structure like this:<br />
        /// - 20220719.zip<br />
        /// - 20220829.7z<br />
        /// - ... (documents)<br /><br />
        /// Library will decide that all documents from the begining of time till 29th Aug 2022 are
        /// packed inside archives and will skip documents with less DateCreatedUtc then found date.
        /// </summary>
        private static DateTime ScanSyncDocumentsDirectoryMinExportDate(string syncDocumentsDirectoryPath)
        {
            const string FILENAME_PATTERN =
                @"(?<YearTo>\d{4})(?<MonthTo>\d{2})(?<DayTo>\d{2})\.(zip|7z)";

            DateTime latestFoundArchiveDate = DateTime.MinValue;

            FileSystemUtils.ScanDirectoryRecursively(syncDocumentsDirectoryPath, f =>
            {
                var match = Regex.Match(f, FILENAME_PATTERN);
                if (match.Success)
                {
                    var year = int.Parse(match.Groups["YearTo"].Value);
                    var month = int.Parse(match.Groups["MonthTo"].Value);
                    var day = int.Parse(match.Groups["DayTo"].Value);
                    var tempDate = new DateTime(
                        year: year,
                        month: month,
                        day: day,
                        hour: 23,
                        minute: 59,
                        second: 59,
                        millisecond: 999,
                        DateTimeKind.Utc
                        );
                    if (tempDate > latestFoundArchiveDate)
                    {
                        latestFoundArchiveDate = tempDate;
#if DEBUG
                        logger.LogDebug($"Archive till '{tempDate}' found.");
#endif
                    }

                }
            });

            return latestFoundArchiveDate;
        }

        /// <summary>
        /// Returnes a HashSet of Id of Documents which are known from <paramref name="fromDate"/>.<br />
        /// <paramref name="fromDate"/> could be calculated by function 
        /// <see cref="ScanSyncDocumentsDirectoryMinExportDate"/>.
        /// </summary>
        private static HashSet<Guid> GetKnownDocumentIds(DateTime fromDate)
        {
            using var dbContext = GetDbContext();
            return dbContext
                .Documents
                .AsNoTracking()
                .Where(d => d.DateCreatedUtc > fromDate)
                .OrderBy(d => d.DateCreatedUtc)
                .Select(d => d.Id)
                .ToHashSet();
        }

        /// <summary>
        /// Scans a <paramref name="syncDocumentsDirectoryPath"/> and returnes a dictionary
        /// where key is a Document.Id (Guid) and value is a full path to file.
        /// </summary>
        private static Dictionary<Guid, string> GetExportedDocumentsIdAndPathDict(string syncDocumentsDirectoryPath)
        {
            const string FILENAME_PATTERN = @"(?<Tickes>\d{18,})_(?<Type>[A-Za-z]+)_" +
                @"(?<DocumentId>[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})";

            var foundDocumentsIdAndPath = new Dictionary<Guid, string>();

            FileSystemUtils.ScanDirectoryRecursively(syncDocumentsDirectoryPath, f =>
            {
                var match = Regex.Match(f, FILENAME_PATTERN);
                if (match.Success)
                {
                    var documentId = Guid.Parse(match.Groups["DocumentId"].Value);
                    foundDocumentsIdAndPath.Add(documentId, f);
                }
            });

            return foundDocumentsIdAndPath;
        }

        /// <summary>
        /// Returnes a list of Document IDs which are need to be exported.<br />
        /// <paramref name="knownDocumentIds"/> - from <see cref="GetKnownDocumentIds(DateTime)"/><br />
        /// <paramref name="exportedDocumentsIdAndPathDict"/> - from <see cref="GetExportedDocumentsIdAndPathDict(string)"/>
        /// </summary>
        private static IEnumerable<Guid> GetDocumentIdsToExportList(
            HashSet<Guid> knownDocumentIds,
            Dictionary<Guid, string> exportedDocumentsIdAndPathDict)
        {
            return knownDocumentIds
                .Where(id => !exportedDocumentsIdAndPathDict.ContainsKey(id))
                .ToList();
        }

        /// <summary>
        /// Returnes a dictionary (Id, Path) of documents which are need to be imported.
        /// <paramref name="knownDocumentIds"/> - from <see cref="GetKnownDocumentIds(DateTime)"/><br />
        /// </summary>
        private static IDictionary<Guid, string> GetDocumentIdAndPathToImportDict(
            HashSet<Guid> knownDocumentIds,
            Dictionary<Guid, string> exportedDocumentsIdAndPathDict)
        {
            return exportedDocumentsIdAndPathDict
                .Where(kvp => !knownDocumentIds.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Retreives documents from <paramref name="documentIdsToExportList"/> and exports them
        /// into directory <paramref name="syncDocumentsDirectoryPath"/>.
        /// </summary>
        private static void ExportDocuments(IEnumerable<Guid> documentIdsToExportList, string syncDocumentsDirectoryPath)
        {
            using var dbContext = GetDbContext();

            foreach (var documentId in documentIdsToExportList)
            {
                var document = dbContext
                    .Documents
                    .AsNoTracking()
                    .Where(d => d.Id == documentId)
                    .Include(d => d.DocumentPayload)
                    .First(); // we are sure it exists

                var documentJsonValue = document.AsJson();

                var syncDocumentsDirectoryWithSubFolderPath = Path.Combine(
                    syncDocumentsDirectoryPath,
                    document.DateCreatedUtc.ToString("yyyyMMdd"));

                if (!FileSystemUtils.TouchDirectory(syncDocumentsDirectoryWithSubFolderPath))
                {
                    logger.LogError($"Can't create directory '{syncDocumentsDirectoryWithSubFolderPath}'. Sync operation stopped");
                    return;
                }

                var shortFileName = $"{document.DateCreatedUtc.Ticks}_{document.DocumentPayload.PayloadTypeName!.FromLatest('.')}_{document.Id}.json";
                var resultPath = Path.Combine(syncDocumentsDirectoryWithSubFolderPath, shortFileName);

                File.WriteAllText(resultPath, documentJsonValue);

                logger.LogTrace($"Document '{document.Id}' exported to '{resultPath}'");
            }
        }

        /// <summary>
        /// Peformes import of documents specified in <paramref name="documentIdAndPathToImportDict"/>.
        /// </summary>
        private static void ImportDocuments(IDictionary<Guid, string> documentIdAndPathToImportDict)
        {
            foreach (var kvp in documentIdAndPathToImportDict)
            {
                var document = FileSystemUtils.TryReadFromFile<Document>(kvp.Value);
                if (document!.Id != kvp.Key)
                {
                    logger.LogWarning($"Id mismatch during deserialization of file '{kvp.Value}'. Skipped");
                    continue;
                }

                var payloadType = ReflectionUtils.FindTypeAccrossAppDomain(document.DocumentPayload.PayloadTypeName!);
                if (payloadType == null)
                {
                    logger.LogError($"Type '{document.DocumentPayload.PayloadTypeName}' from '{document.Id}' doesn't exists in current app domain");
                    continue;
                }

                var dto = new DtoOf<object>
                {
                    Id = kvp.Key,
                    UserId = document.UserId,
                    RequiredDocumentId = document.RequiredDocumentId,
                    DateCreatedUtc = document.DateCreatedUtc,
                    Payload = FileSystemUtils.GetSerializer().Deserialize(document.DocumentPayload.PayloadJson!, payloadType),
                };

                var pushResult = PushDto(dto);

                if (pushResult.IsSuccess())
                    logger.LogInformation($"Document '{dto.Id}' imported");
                else
                    logger.LogError($"Unable to import document from file '{kvp.Value}': " +
                        $"got '{pushResult.GetType().Name}' ({pushResult.AsJson()})");
            }
        }

        #endregion

        #endregion

        #region DEBUG-only functions

#if DEBUG
        /// <summary>
        /// Dropes existing database and creates a new one.<br />
        /// Known payload types will be recalculated.
        /// <b>FOR TESTS ONLY !!!</b>
        /// </summary>
        public static void RecreateDatabase()
        {
            if (!_isInitialized) throw new ApplicationException(ERROR_NOT_INITIALIZED);

            using var dbContext = GetDbContext();
            if (dbContext is DbContext ctx)
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }
            else
            {
                throw new InvalidCastException(nameof(DbContext));
            }
        }
#endif

        #endregion

    }
}