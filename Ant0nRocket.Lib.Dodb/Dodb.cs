﻿using Ant0nRocket.Lib.Dodb.DbContexts;
using Ant0nRocket.Lib.Dodb.Dto;
using Ant0nRocket.Lib.Dodb.Dto.Payloads.Abstractions;
using Ant0nRocket.Lib.Dodb.Enums;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Helpers;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Models;
using Ant0nRocket.Lib.Dodb.Serialization;
using Ant0nRocket.Lib.Std20;
using Ant0nRocket.Lib.Std20.Cryptography;
using Ant0nRocket.Lib.Std20.Extensions;
using Ant0nRocket.Lib.Std20.IO;
using Ant0nRocket.Lib.Std20.Logging;
using Ant0nRocket.Lib.Std20.Reflection;

using Microsoft.EntityFrameworkCore;

using System.Text.RegularExpressions;

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

        private static Mutex mutexForPushDto = new();

        private static readonly Dictionary<string, int> _payloadType_Id_Cache = new();

        #endregion

        #region Events

        /// <summary>
        /// When there is a success document write to database.
        /// </summary>
        public static event EventHandler<long>? OnDatabaseVersionUpdate;

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
            if (_isInitialized)
                return;

            Ant0nRocketLibConfig.RegisterJsonSerializer(new NewtonsoftJsonSerializer());

            _getDbContextHandler = getDbContextHandler ?? throw new NullReferenceException(nameof(getDbContextHandler));
            _dtoPayloadHandler = dtoPayloadHandler ?? throw new NullReferenceException(nameof(dtoPayloadHandler));

            // Important to set this flag here! TouchUser will throw if false.
            _isInitialized = true;
        }

        #endregion

        #region DTO handling

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

            var dtoHandleResponse =
                TryHandleDtoPayloadExternally(dtoPayload, dbContext) ??
                new GrDtoPushFailed { Reason = GrPushFailReason.PayloadHandlerNotFound, Dto = dto };

            if (dtoHandleResponse is GrDtoPushSuccess success && success.SkipDocumentCreation)
                return dtoHandleResponse; // prevent creating a document, because of flag SkipDocumentCreation

            var document = new Document
            {
                Id = dto.Id,
                UserId = dto.UserId,
                RequiredDocumentId = dto.RequiredDocumentId,
                DateCreatedUtc = dto.DateCreatedUtc,
                Description = dto.Description,
                PayloadJson = dtoPayload.AsJson(pretty: false),
                PayloadTypeName = dtoPayload.GetType().FullName,
            };

            dbContext.Documents.Add(document);
            return dtoHandleResponse;
        }

        /// <summary>
        /// Performs pushing of a DTO.
        /// </summary>
        public static IGatewayResponse PushDto(DtoBase dto)
        {
            if (!_isInitialized)
                throw new ApplicationException(ERROR_NOT_INITIALIZED);

            #region Basic validation (will check properties according to annotations)

            var validator = new DtoValidator(dto).Validate();
            if (validator.ValidationResults.Count > 0)
            {
                var response = new GrDtoPushFailed
                {
                    Reason = GrPushFailReason.ValidationFailed,
                    Dto = dto,
                };
                validator.ValidationResults.ForEach(r => response.Messages.Add(r.ErrorMessage!));
                return response;
            }

            #endregion

            using var dbContext = GetDbContext();
            using var transaction = dbContext.Database.BeginTransaction();

            #region Database validations

            if (dbContext.Documents.Any(d => d.Id == dto.Id))
            {
                return new GrDtoPushFailed { Reason = GrPushFailReason.DocumentExists, Dto = dto };
            }

            if (dto.RequiredDocumentId != null)
            {
                if (!dbContext.Documents.Any(d => d.Id == dto.RequiredDocumentId))
                {
                    return new GrDtoPushFailed
                    {
                        Reason = GrPushFailReason.RequiredDocumentNotExists,
                        Dto = dto
                    };
                }
            }
            else // RequiredDocumentId is NOT specified
            {
                // This situation could be ONLY when there is a first document. 
                // So, if any document exists there should not be a DTO without RequiredDocumentId
                if (dbContext.Documents.Any())
                {
                    return new GrDtoPushFailed(dto, "RequiredDocumentId must be specified!");
                }
            }

            #endregion


            try
            {
                mutexForPushDto.WaitOne(); // Take a mutex (or wait here until busy)

                var pushResult = PushDtoObject(dto, dbContext);

                if (pushResult is GrDtoPushSuccess)
                {
                    dbContext.SaveChanges();
                    transaction?.Commit();
                    logger.LogInformation($"DTO '{dto.Id}' applied to database");
                    OnDatabaseVersionUpdate?.Invoke(null, dto.DateCreatedUtc.Ticks);
                    return pushResult;
                }
                else if (pushResult is GrDtoPushFailed failedResult)
                {
                    logger.LogObject(failedResult);
                    return failedResult;
                }
                else
                {
                    var message = $"Push result '{pushResult.GetType().Name}' is not inherited from" +
                        $"{nameof(GrDtoPushFailed)} or {nameof(GrDtoPushSuccess)}: fix that! " +
                        $"Current transaction rolled back.";

                    logger.LogError(message);
                    return new GrDtoPushFailed(dto, message)
                    {
                        Reason = GrPushFailReason.UnknownResultType
                    };
                }
            }
            catch (Exception ex)
            {
                var message = ex.GetFullExceptionErrorMessage();
                logger.LogError(message);
                var result = new GrDtoPushFailed(dto, message, GrPushFailReason.DatabaseError)
                {
#if DEBUG
                    DbContextDebugViewLong = dbContext?.ChangeTracker.DebugView.LongView,
                    DbContextDebugViewShort = dbContext?.ChangeTracker.DebugView.ShortView,
#endif
                };
                return result;
            }
            finally
            {
                // Transaction and dbcontext will be disposed automatically.
                // If transaction were not commited - rollback will be called.
                mutexForPushDto.ReleaseMutex(); // release mutex
            }
        }

        /// <summary>
        /// <inheritdoc cref="PushDto(DtoBase)" />
        /// </summary>
        public static async Task<IGatewayResponse> PushDtoAsync(DtoBase dto) =>
            await Task.Run(() => PushDto(dto));

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
            if (!_isInitialized)
                throw new ApplicationException(ERROR_NOT_INITIALIZED);

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

        #endregion

        #region Synchronization

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

                mutexForPushDto.WaitOne();
                logger.LogInformation($"Documents synchronization started...");

                PerformSyncDocumentsIteration(syncDocumentsDirectoryPath);

                mutexForPushDto.ReleaseMutex();
                logger.LogInformation($"Documents synchronization finished.");
            }
        }

        private static void PerformSyncDocumentsIteration(string syncDocumentsDirectoryPath)
        {
            var exportFromDate = Stage1_ScanSyncDocumentsDirectoryMinExportDate(syncDocumentsDirectoryPath);
            logger.LogInformation($"Operational period begin is {exportFromDate}");

            var knownDocumentIds = Stage2_GetKnownDocumentIds(exportFromDate);
            logger.LogInformation($"Database has {knownDocumentIds.Count} documents for specified period");

            var exportedDocumentsIdAndPathDict = Stage3_GetExportedDocumentsIdAndPathDict(syncDocumentsDirectoryPath);
            logger.LogInformation($"Already exported documents count is {exportedDocumentsIdAndPathDict.Count}");

            var documentIdsToExportList = Stage4_GetDocumentIdsToExportList(knownDocumentIds, exportedDocumentsIdAndPathDict);
            logger.LogInformation($"Documents to export count is {documentIdsToExportList.Count()}");

            var documentIdAndPathToImportDict = Stage5_GetDocumentIdAndPathToImportDict(
                knownDocumentIds,
                exportedDocumentsIdAndPathDict);
            logger.LogInformation($"Documents to import count is {documentIdAndPathToImportDict.Count}");

            logger.LogInformation("Documents export began...");
            Stage6_ExportDocuments(documentIdsToExportList, syncDocumentsDirectoryPath);
            logger.LogInformation("Documents export finished.");

            logger.LogInformation("Documents import began...");
            Stafe7_ImportDocuments(documentIdAndPathToImportDict);
            logger.LogInformation("Documents import finished.");
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
        private static DateTime Stage1_ScanSyncDocumentsDirectoryMinExportDate(string syncDocumentsDirectoryPath)
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
                    }

                }
            });

            return latestFoundArchiveDate;
        }

        /// <summary>
        /// Returnes a HashSet of Id of Documents which are known from <paramref name="fromDate"/>.<br />
        /// <paramref name="fromDate"/> could be calculated by function 
        /// <see cref="Stage1_ScanSyncDocumentsDirectoryMinExportDate"/>.
        /// </summary>
        private static HashSet<Guid> Stage2_GetKnownDocumentIds(DateTime fromDate)
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
        private static Dictionary<Guid, string> Stage3_GetExportedDocumentsIdAndPathDict(string syncDocumentsDirectoryPath)
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
        /// <paramref name="knownDocumentIds"/> - from <see cref="Stage2_GetKnownDocumentIds(DateTime)"/><br />
        /// <paramref name="exportedDocumentsIdAndPathDict"/> - from <see cref="Stage3_GetExportedDocumentsIdAndPathDict(string)"/>
        /// </summary>
        private static IEnumerable<Guid> Stage4_GetDocumentIdsToExportList(
            HashSet<Guid> knownDocumentIds,
            Dictionary<Guid, string> exportedDocumentsIdAndPathDict)
        {
            return knownDocumentIds
                .Where(id => !exportedDocumentsIdAndPathDict.ContainsKey(id))
                .ToList();
        }

        /// <summary>
        /// Returnes a dictionary (Id, Path) of documents which are need to be imported.
        /// <paramref name="knownDocumentIds"/> - from <see cref="Stage2_GetKnownDocumentIds(DateTime)"/><br />
        /// </summary>
        private static IDictionary<Guid, string> Stage5_GetDocumentIdAndPathToImportDict(
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
        private static void Stage6_ExportDocuments(IEnumerable<Guid> documentIdsToExportList, string syncDocumentsDirectoryPath)
        {
            using var dbContext = GetDbContext();

            foreach (var documentId in documentIdsToExportList)
            {
                var document = dbContext
                    .Documents
                    .AsNoTracking()
                    .Where(d => d.Id == documentId)
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

                var shortFileName = $"{document.DateCreatedUtc.Ticks}_{document.PayloadTypeName!.FromLatest('.')}_{document.Id}.json";
                var resultPath = Path.Combine(syncDocumentsDirectoryWithSubFolderPath, shortFileName);

                File.WriteAllText(resultPath, documentJsonValue);
                logger.LogInformation($"Document '{documentId}' have been exported.");
            }
        }

        /// <summary>
        /// Peformes import of documents specified in <paramref name="documentIdAndPathToImportDict"/>.
        /// </summary>
        private static void Stafe7_ImportDocuments(IDictionary<Guid, string> documentIdAndPathToImportDict)
        {
            foreach (var kvp in documentIdAndPathToImportDict)
            {
                var document = FileSystemUtils.TryReadFromFile<Document>(kvp.Value);
                if (document!.Id != kvp.Key)
                {
                    logger.LogWarning($"Id mismatch during deserialization of file '{kvp.Value}'. Skipped");
                    continue;
                }

                var payloadType = ReflectionUtils.FindTypeAccrossAppDomain(document.PayloadTypeName!);
                if (payloadType == null)
                {
                    logger.LogError($"Type '{document.PayloadTypeName}' from '{document.Id}' doesn't exists in current app domain");
                    continue;
                }

                var dto = new DtoOf<object>
                {
                    Id = kvp.Key,
                    UserId = document.UserId,
                    RequiredDocumentId = document.RequiredDocumentId,
                    Description = document.Description,
                    DateCreatedUtc = document.DateCreatedUtc,
                    Payload = Ant0nRocketLibConfig.GetJsonSerializer().Deserialize(
                        contents: document.PayloadJson!,
                        type: payloadType!,
                        throwExceptions: true),
                };

                var pushResult = PushDto(dto); // we are in our thread, mutex locked, so it's ok

                if (pushResult is GrDtoPushSuccess)
                {
                    logger.LogInformation($"Documents '{dto.Id}' have been imported.");
                }
                else if (pushResult is GrDtoPushFailed f)
                {
                    logger.LogError($"Unable to import document from file '{kvp.Value}': {f.Messages.AsJson()}");
                }
                else
                {
                    logger.LogFatal($"UNKNOWN PUSH RESULT '{pushResult.GetType().Name}");
                }
            }
        }

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
            if (!_isInitialized)
                throw new ApplicationException(ERROR_NOT_INITIALIZED);

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