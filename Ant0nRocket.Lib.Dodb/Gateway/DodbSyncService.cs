using System.Text.RegularExpressions;

using Ant0nRocket.Lib.Dodb.Attributes;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Std20.Extensions;
using Ant0nRocket.Lib.Std20.IO;
using Ant0nRocket.Lib.Std20.Logging;
using Ant0nRocket.Lib.Std20.Reflection;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    public static class DodbSyncService
    {
        private static readonly Logger logger = Logger.Create(nameof(DodbSyncService));

        public static event Action OnSyncStarted;
        public static event Action<(long current, long total)> OnSyncProgress;
        public static event Action OnSyncCompleted;
        public static event Action<string> OnSyncError;

        private static string syncDirectoryPath = default;

        public static void SetSyncDirectoryPath(string value)
        {
            syncDirectoryPath = value;
            FileSystemUtils.TouchDirectory(value);
        }

        public static void Sync()
        {
            if (syncDirectoryPath == default)
            {
                const string ERROR_MESSAGE = "SyncDirectoryPath is not set";
                OnSyncError?.Invoke(ERROR_MESSAGE);
                logger.LogError(ERROR_MESSAGE);
            }
            else
            {
                OnSyncStarted?.Invoke();
                PerformSyncIteration();
                OnSyncCompleted?.Invoke();
            }
        }

        private static void PerformSyncIteration()
        {
            var workingDate = ScanSyncDirectoryAndFindMinimumExportDate();
            var knownDocumentIdHashSet = GetKnownDocumentIdHashSet(workingDate);
            var syncDirectoryDocumentsIdAndPath = GetSyncDirectoryDocumentsIdAndPath();
            var documentsToExportList = GetDocumentsToExportList(knownDocumentIdHashSet, syncDirectoryDocumentsIdAndPath);
            var documentsToImportDict = GetDocumentsToImportDict(knownDocumentIdHashSet, syncDirectoryDocumentsIdAndPath);
            ExportDocuments(documentsToExportList);
            ImportDocuments(documentsToImportDict);
        }

        private static DateTime ScanSyncDirectoryAndFindMinimumExportDate()
        {
            const string FILENAME_PATTERN =
                @"(?<YearFrom>\d{4})(?<MonthFrom>\d{2})(?<DayFrom>\d{2})_(?<YearTo>\d{4})(?<MonthTo>\d{2})(?<DayTo>\d{2})\.(zip|7z)";

            var result = DateTime.MinValue;

            FileSystemUtils.ScanDirectoryRecursively(syncDirectoryPath, f =>
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
                    if (tempDate > result) result = tempDate;

#if DEBUG
                    logger.LogDebug($"Archive till '{tempDate}' found.");
#endif
                }
            });

            return result;
        }


        /// <summary>
        /// Returnes a HashSet of Id of known documents (existing in database)
        /// </summary>
        private static HashSet<Guid> GetKnownDocumentIdHashSet(DateTime fromDate)
        {
            using var dbContext = DodbGateway.GetContext();
            return dbContext
                .Documents
                .AsNoTracking()
                .Where(d => d.DateCreatedUtc > fromDate && d.IsDeleted == false)
                .OrderBy(d => d.DateCreatedUtc)
                .Select(d => d.Id)
                .ToHashSet();
        }

        /// <summary>
        /// Scans a syncDirectoryPath and returnes a Dictionary
        /// where key is a DocumentId (Guid) and value is a full path to file.
        /// </summary>
        private static Dictionary<Guid, string> GetSyncDirectoryDocumentsIdAndPath()
        {
            const string FILENAME_PATTERN =
                @"(?<Year>\d{4})(?<Month>\d{2})(?<Day>\d{2})_" +
                @"(?<Hours>\d{2})(?<Minutes>\d{2})(?<Seconds>\d{2})(?<MilliSeconds>\d+)_" +
                @"(?<DocumentId>[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})";

            var foundDocumentsIdAndPath = new Dictionary<Guid, string>();
            FileSystemUtils.TouchDirectory(syncDirectoryPath); // just to sure
            FileSystemUtils.ScanDirectoryRecursively(syncDirectoryPath, f =>
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
        /// Returnes a list of documents (Id) which are need to be exported
        /// </summary>
        private static IEnumerable<Guid> GetDocumentsToExportList(HashSet<Guid> knownDocumentIdHashSet, Dictionary<Guid, string> syncDirectoryDocumentsIdAndPath) =>
            knownDocumentIdHashSet.Where(id => !syncDirectoryDocumentsIdAndPath.ContainsKey(id)).ToList();

        /// <summary>
        /// Returnes a dictionary (Id, Path) of documents which are need to be imported
        /// </summary>
        private static IDictionary<Guid, string> GetDocumentsToImportDict(HashSet<Guid> knownDocumentIdHashSet, Dictionary<Guid, string> syncDirectoryDocumentsIdAndPath) =>
            syncDirectoryDocumentsIdAndPath
                .Where(kvp => !knownDocumentIdHashSet.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        /// <summary>
        /// Exports specified documents to syncDirectoryPath
        /// </summary>
        private static void ExportDocuments(IEnumerable<Guid> documentsToExportList)
        {
            using var dbContext = DodbGateway.GetContext();

            foreach (var documentId in documentsToExportList)
            {
                var document = dbContext
                    .Documents
                    .AsNoTracking()
                    .Where(d => d.Id == documentId)
                    .First(); // we are sure it exists

                var documentJsonValue = document.AsJson();

                var syncDirectoryWithSubFolderPath = Path.Combine(
                    syncDirectoryPath,
                    document.DateCreatedUtc.ToString("yyyyMMdd"));

                if (!FileSystemUtils.TouchDirectory(syncDirectoryWithSubFolderPath))
                {
                    logger.LogError($"Can't create directory '{syncDirectoryWithSubFolderPath}'. Sync operation stopped");
                    return;
                }

                var shortFileName = $"{document.DateCreatedUtc:yyyyMMdd}_{document.DateCreatedUtc:HHmmssfffffff}_{document.Id}.json";
                var resultPath = Path.Combine(syncDirectoryWithSubFolderPath, shortFileName);

                File.WriteAllText(resultPath, documentJsonValue);

                logger.LogTrace($"Document '{document.Id}' exported to '{resultPath}'");
            }
        }

        private static void ImportDocuments(IDictionary<Guid, string> documentsToImportDict)
        {
            foreach (var kvp in documentsToImportDict)
            {
                var document = FileSystemUtils.TryReadFromFile<Document>(kvp.Value);
                if (document.Id != kvp.Key)
                {
                    logger.LogWarning($"Id mismatch during deserialization of file '{kvp.Value}'. Skipped");
                    continue;
                }

                var payloadType = GetTypeAccrossAppDomain(document.PayloadType);
                if (payloadType == null)
                {
                    logger.LogError($"Type '{document.PayloadType}' from '{document.Id}' doesn't exists in current app domain");
                    continue;
                }

                var dto = new DtoOf<object>
                {
                    Id = kvp.Key,
                    AuthToken = document.AuthorId, // gateway will use AuthorId to store it in database
                    RequiredDocumentId = document.RequiredDocumentId,
                    DateCreatedUtc = document.DateCreatedUtc,
                    Payload = FileSystemUtils.GetSerializer().Deserialize(document.Payload, payloadType),
                };

                var pushResult = DodbGateway.PushDto(
                    dto: dto, 
                    skipAuthTokenValidation: true,
                    onDocumentCreated: d => d.AuthorId = dto.AuthToken);

                var isPushResultSuccessful = AttributeUtils
                    .GetAttribute<IsSuccessAttribute>(pushResult.GetType())?.IsSuccess ?? 
                    true; // by default operation mean to be successful (non-successful are marked in attribute)

                if (isPushResultSuccessful)
                    logger.LogInformation($"Document '{dto.Id}' imported");
                else
                    logger.LogError($"Unable to import document from file '{kvp.Value}': " +
                        $"got '{pushResult.GetType().Name}' ({pushResult.AsJson()})");
            }
        }

        private static Type GetTypeAccrossAppDomain(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                var targetType = types.Where(t => t.FullName == typeName).FirstOrDefault();
                if (targetType != null)
                    return targetType;
            }

            return null;
        }
    }
}
