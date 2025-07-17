using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.IO;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    public class T003_DodbGateway_Sync : DodbTestsBase
    {
        [Test]
        public async Task T008_CleanupSyncDirectoryAndSyncAgain()
        {
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => File.Delete(f)); // clean up
            await Dodb.SyncDocumentsAsync(syncDirectory); // should create 2 files

            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.That(filesList.Count == 2);
        }

        [Test]
        public async Task T009_CheckSyncPopulateDatabase()
        {
            // Make sure we have 4 files from prev. test
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.That(filesList.Count == 2);

            using var dbContext = new TestDbContext();
            dbContext.Documents.RemoveRange(dbContext.Documents);
            dbContext.Users.RemoveRange(dbContext.Users);
            dbContext.SaveChanges();
            Assert.That(dbContext.Documents.Any() == false);

            await Dodb.SyncDocumentsAsync(syncDirectory);

            Assert.That(dbContext.Documents.Count() == 2);
        }
    }
}
