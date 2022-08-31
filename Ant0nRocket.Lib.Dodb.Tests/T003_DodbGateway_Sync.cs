﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Std20.IO;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    public class T003_DodbGateway_Sync : DodbTestsBase
    {
        [Test]
        public void T008_CleanupSyncDirectoryAndSyncAgain()
        {
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => File.Delete(f)); // clean up
            DodbGateway.SyncDocuments(syncDirectory); // should create 2 files

            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.AreEqual(2, filesList.Count);
        }

        [Test]
        public void T009_CheckSyncPopulateDatabase()
        {
            // Make sure we have 4 files from prev. test
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.AreEqual(2, filesList.Count);

            using var dbContext = new TestDbContext();
            dbContext.Documents.RemoveRange(dbContext.Documents);
            dbContext.Users.RemoveRange(dbContext.Users);
            dbContext.SaveChanges();
            Assert.AreEqual(false, dbContext.Documents.Any());

            DodbGateway.SyncDocuments(syncDirectory);

            Assert.AreEqual(2, dbContext.Documents.Count());
        }
    }
}