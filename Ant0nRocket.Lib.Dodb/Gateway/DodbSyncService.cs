using Ant0nRocket.Lib.Std20.Extensions;
using Ant0nRocket.Lib.Std20.IO;
using Ant0nRocket.Lib.Std20.Logging;

using Microsoft.EntityFrameworkCore;

using System.Text.RegularExpressions;

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
            if (!Directory.Exists(syncDirectoryPath))
                Directory.CreateDirectory(syncDirectoryPath);
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
            var knownDocumentIdHashSet = GetKnownDocumentIdHashSet();
            var syncDirectoryDocumentsIdAndPath = GetSyncDirectoryDocumentsIdAndPath();
            var documentsToExportList = GetDocumentsToExportList(knownDocumentIdHashSet, syncDirectoryDocumentsIdAndPath);
            var documentsToImportDict = GetDocumentsToImportDict(knownDocumentIdHashSet, syncDirectoryDocumentsIdAndPath);
            ExportDocuments(documentsToExportList);
            //ImportDocuments(documentsToImportDict);
        }

        private static readonly HashSet<Guid> knownDocumentIdHashSet = new();

        /// <summary>
        /// Returnes a HashSet of Id of known documents (existing in database)
        /// </summary>
        private static HashSet<Guid> GetKnownDocumentIdHashSet()
        {
            if (knownDocumentIdHashSet.Count == 0)
            {
                using var dbContext = DodbGateway.GetContext();
                dbContext
                    .Documents
                    .AsNoTracking()
                    .Where(d => d.IsDeleted == false)
                    .OrderBy(d => d.DateCreatedUtc)
                    .Select(d => d.Id)
                    .ToList()
                    .ForEach(e => knownDocumentIdHashSet.Add(e));
            }
            return knownDocumentIdHashSet;
        }

        /// <summary>
        /// Scans a syncDirectoryPath and returnes a Dictionary
        /// where key is a DocumentId (Guid) and value is a full path to file.
        /// </summary>
        private static Dictionary<Guid, string> GetSyncDirectoryDocumentsIdAndPath()
        {
            const string FILENAME_PATTERN =
                @"(?<Year>\d{4})(?<Month>\d{2})(?<Day>\d{2})_" +
                @"(?<Hours>\d{2})(?<Minutes>\d{2})(?<Seconds>\d{2})_" +
                @"(?<DocumentId>[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})";

            var foundDocumentsIdAndPath = new Dictionary<Guid, string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectoryPath, f =>
            {
                var match = Regex.Match(f, FILENAME_PATTERN);
                if (match.Success)
                {
                    var documentId = Guid.Parse(match.Groups["DocumentId"].Value);
                    foundDocumentsIdAndPath.Add(documentId, f);

                    //var year = int.Parse(match.Groups["Year"].Value);
                    //var month = int.Parse(match.Groups["Month"].Value);
                    //var day = int.Parse(match.Groups["Day"].Value);
                    //var hours = int.Parse(match.Groups["Hours"].Value);
                    //var minutes = int.Parse(match.Groups["Minutes"].Value);
                    //var seconds = int.Parse(match.Groups["Seconds"].Value);
                    //var dateUtc = new DateTime(year, month, day, hours, minutes, seconds, DateTimeKind.Utc);

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
        /// <param name="documentsToExportList"></param>
        /// <exception cref="NotImplementedException"></exception>
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
                var syncDirectorySubFolderPath = document.DateCreatedUtc.ToString("yyyyMMdd");
                var shortFileName = $"{document.DateCreatedUtc:yyyyMMdd}_{document.DateCreatedUtc:HHmmss}_{document.Id}";
                var resultPath = Path.Combine(syncDirectoryPath, syncDirectorySubFolderPath, shortFileName);
                FileSystemUtils.TouchDirectory(resultPath);
                File.WriteAllText(resultPath, documentJsonValue);
                logger.LogTrace($"Document '{document.Id}' exported to '{resultPath}'");
            }
        }

        private static void ImportDocuments(IDictionary<Guid, string> documentsToImportDict)
        {
            throw new NotImplementedException();
        }
    }
}
