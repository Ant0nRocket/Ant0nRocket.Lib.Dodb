using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Entities;
using Ant0nRocket.Lib.Std20.IO;
using Ant0nRocket.Lib.Std20.Logging;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    public class T002_DodbGateway
    {
        private static User? rootUser;

        [Test]
        public void T000_AuthAsRoot()
        {
            T001_DodbUsersServiceTests.AuthUser("__root", "root", out rootUser);
            Assert.That(rootUser is not null);
        }

        [Test]
        public void T001_SendingUnHandledDtoType()
        {
            var dto = new DtoOf<NotHandledPayload>() { UserId = Guid.NewGuid() };
            var result = DodbGateway.PushDto(dto);
            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoFromUnknownUser);
        }

        [Test]
        public void T002_SendingDtoWithInvalidDtoProperties()
        {
            var dto = new DtoOf<TestPayload>() { Id = Guid.Empty };
            var result = DodbGateway.PushDto(dto);
            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoIsInvalid);
        }

        [Test]
        public void T003_1_SendingDtoWithAnnotationValidationErrors()
        {
            var dto = new DtoOf<AnnotatedPayload>
            {
                Payload = new()
                {
                    SomeIntValue = -10,
                    SomeStringValue = "Hello world"
                },
                UserId = Guid.NewGuid(), // mock, for passing basic validation
                DateCreatedUtc = DateTime.Now // same reason
            };

            var result = DodbGateway.PushDto(dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoIsInvalid);
            Assert.That((result as GrDtoIsInvalid).Errors.Count == 2);
        }

        [Test]
        public void T003_2_SendingDtoWithIValidatableImplementation()
        {
            var dto = new DtoOf<ValidatablePayload>
            {
                Payload = new() { TestValue = 11 },
                UserId = Guid.NewGuid(), // mock, for passing basic validation
                DateCreatedUtc = DateTime.Now // same reason
            };

            var result = DodbGateway.PushDto(dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoIsInvalid);
            Assert.That((result as GrDtoIsInvalid).Errors.Count == 1);
        }

        [Test]
        public void T004_SendValidDto()
        {
            var dto = DodbGateway.CreateDto<AnnotatedPayload>(userId: rootUser?.Id ?? default);
            dto.Payload.SomeIntValue = 36;
            dto.Payload.SomeStringValue = "Anton";

            var result = DodbGateway.PushDto(dto);
            Assert.That(result is GrOk);

            dto = DodbGateway.CreateDto<AnnotatedPayload>(userId: rootUser?.Id ?? default);
            dto.Payload.SomeIntValue = 35;
            dto.Payload.SomeStringValue = "Olga";

            result = DodbGateway.PushDto(dto);
            Assert.That(result is GrOk);
        }

        [Test]
        public void T007_ValidationOfLists()
        {
            var dto = DodbGateway.CreateDto<ListPayload>(userId: rootUser?.Id ?? default);
            var pushResult = DodbGateway.PushDto(dto);
            Assert.That(pushResult is GrDtoIsInvalid); // no items added

            // Valid
            dto.Payload.Items.Add(new() { SomeIntValue = 10, SomeStringValue = "12" });
            pushResult = DodbGateway.PushDto(dto);
            Assert.That(pushResult is GrOk);

            // Invalid
            dto.Payload.Items.Clear();
            dto.Payload.Items.Add(new() { SomeIntValue = -10, SomeStringValue = "12" }); // -10 is invalid
            pushResult = DodbGateway.PushDto(dto);
            Assert.That(pushResult is GrDtoIsInvalid);

            // Invalid + valid = invalid
            dto.Payload.Items.Clear();
            dto.Payload.Items.Add(new() { SomeIntValue = 10, SomeStringValue = "12" }); // valid
            dto.Payload.Items.Add(new() { SomeIntValue = -10, SomeStringValue = "12" }); // valid
            pushResult = DodbGateway.PushDto(dto);
            Assert.That(pushResult is GrDtoIsInvalid);
        }

        [Test]
        public void T008_CleanupSyncDirectoryAndSyncAgain()
        {
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => File.Delete(f)); // clean up
            DodbSyncService.SyncDocuments(syncDirectory); // should create 4 files

            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.AreEqual(4, filesList.Count);
        }

        [Test]
        public void T009_CheckSyncPopulateDatabase()
        {
            // Make sure we have 4 files from prev. test
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.AreEqual(4, filesList.Count);

            using var dbContext = new TestDbContext();
            dbContext.Documents.RemoveRange(dbContext.Documents);
            dbContext.Users.RemoveRange(dbContext.Users);
            dbContext.SaveChanges();
            Assert.AreEqual(false, dbContext.Documents.Any());

            DodbSyncService.SyncDocuments(syncDirectory);

            Assert.AreEqual(4, dbContext.Documents.Count());
        }

        [Test]
        public void T010_TryLoadPlugin()
        {
            var fn = BasicLogWritter.CurrentFileName;

            var pluginLaunched = false;
            var pluginWorkComplete = false;
            var pluginError = false;

            DodbSyncService.OnSyncPluginBeforeLaunch += (s, e) => pluginLaunched = true;
            DodbSyncService.OnSyncPluginWorkComplete += (s, e) => pluginWorkComplete = true;
            DodbSyncService.OnSyncPluginError += (s, e) => pluginError = true;

            DodbSyncService.SyncPlugins();

            Assert.That(pluginLaunched);
            Assert.That(pluginWorkComplete);
            Assert.That(pluginError is false);
        }
    }
}
