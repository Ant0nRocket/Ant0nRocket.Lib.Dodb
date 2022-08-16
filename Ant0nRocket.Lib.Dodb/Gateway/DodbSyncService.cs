using System.Text.RegularExpressions;

using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.EventArgs;
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

        #region Sync documents

        public static event EventHandler<string> OnSyncDocumentsStarted;
        public static event EventHandler<DateTime> OnSyncDocumentsArchiveFound;
        public static event EventHandler<string> OnSyncDocumentsCompleted;
        public static event EventHandler<string> OnSyncError;

        /// <summary>
        /// Performes syncthronization of known Documents inside <paramref name="syncDocumentsDirectoryPath"/>.<br />
        /// </summary>
        public static void SyncDocuments(string syncDocumentsDirectoryPath)
        {
            if (syncDocumentsDirectoryPath == default)
            {
                const string ERROR_MESSAGE = "SyncDirectoryPath is not set";
                OnSyncError?.Invoke(null, ERROR_MESSAGE);
                logger.LogError(ERROR_MESSAGE);
            }
            else
            {
                FileSystemUtils.TouchDirectory(syncDocumentsDirectoryPath); // just to sure
                OnSyncDocumentsStarted?.Invoke(null, syncDocumentsDirectoryPath);
                PerformSyncDocumentsIteration(syncDocumentsDirectoryPath);
                OnSyncDocumentsCompleted?.Invoke(null, syncDocumentsDirectoryPath);
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
                        OnSyncDocumentsArchiveFound?.Invoke(null, latestFoundArchiveDate);
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
        /// Scans a <paramref name="syncDocumentsDirectoryPath"/> and returnes a dictionary
        /// where key is a Document.Id (Guid) and value is a full path to file.
        /// </summary>
        private static Dictionary<Guid, string> GetExportedDocumentsIdAndPathDict(string syncDocumentsDirectoryPath)
        {
            const string FILENAME_PATTERN =
                @"(?<Year>\d{4})(?<Month>\d{2})(?<Day>\d{2})_" +
                @"(?<Hours>\d{2})(?<Minutes>\d{2})(?<Seconds>\d{2})(?<MilliSeconds>\d+)_" +
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
            using var dbContext = DodbGateway.GetContext();

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

                var shortFileName = $"{document.DateCreatedUtc:yyyyMMdd}_{document.DateCreatedUtc:HHmmssfffffff}_{document.Id}.json";
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
                if (document.Id != kvp.Key)
                {
                    logger.LogWarning($"Id mismatch during deserialization of file '{kvp.Value}'. Skipped");
                    continue;
                }

                var payloadType = ReflectionUtils.FindTypeAccrossAppDomain(document.PayloadType);
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

        #endregion

        #region Sync plugins

        public static event EventHandler<IDodbSyncServicePlugin> OnSyncPluginBeforeLaunch;
        public static event EventHandler<IDodbSyncServicePlugin> OnSyncPluginWorkComplete;
        public static event EventHandler<SyncPluginErrorEventArgs> OnSyncPluginError;

        public static void SyncPlugins()
        {
            var knownPluginsTypes = ReflectionUtils.GetClassesThatImplementsInterface<IDodbSyncServicePlugin>();
            foreach (var pluginType in knownPluginsTypes)
            {
                var pluginInstance = (IDodbSyncServicePlugin)Activator.CreateInstance(pluginType);
                logger.LogInformation($"Found plugin '{pluginInstance.Name}'. Ready state: {pluginInstance.IsReady}.");
                logger.LogInformation($"Starting Sync method of plugin '{pluginInstance.Name}'");

                try
                {
                    OnSyncPluginBeforeLaunch?.Invoke(null, pluginInstance);
                    if (pluginInstance.IsReady)
                    {
                        pluginInstance.Sync();
                    }
                    else
                    {
                        logger.LogWarning($"Plugin '{pluginInstance.Name}' is not ready. Skip sync action.");
                    }
                    OnSyncPluginWorkComplete?.Invoke(null, pluginInstance);
                }
                catch (Exception ex)
                {                    
                    logger.LogException(ex);
                    OnSyncPluginError?.Invoke(null, new SyncPluginErrorEventArgs { Plugin = pluginInstance, Exception = ex });
                }
            }
        }

        #endregion
    }
}
