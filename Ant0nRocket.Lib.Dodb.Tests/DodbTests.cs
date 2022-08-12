
using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Entities;
using Ant0nRocket.Lib.Dodb.Tests.Services;
using Ant0nRocket.Lib.Std20;
using Ant0nRocket.Lib.Std20.IO;
using Ant0nRocket.Lib.Std20.Logging;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ant0nRocket.Lib.Dodb.Tests
{

    public class DodbTests
    {
        [Test]
        public void T001_RegisterGetterAndHandlers()
        {
            Ant0nRocketLibConfig.IsPortableMode = true;
            Logger.LogToBasicLogWritter = true;

            DodbGateway.RegisterContextGetter(new Func<IDodbContext>(() => new TestDbContext()));
            DodbGateway.RegisterDtoHandler(DtoHandlerMethod);

        }

        private GatewayResponse DtoHandlerMethod(object dtoPayloadObject, IDodbContext dbContext)
        {
            return dtoPayloadObject switch
            {
                TestPayload dtoPayload => TestService.TestMethod(dtoPayload, dbContext),
                AnnotatedPayload p => new GrOk(),
                ListPayload p => new GrOk(),
                _ => new GrDtoPayloadHandlerNotFound()
            };
        }

        [Test]
        public void T002_SendingUnHandledDtoType()
        {
            var dto = new DtoOf<NotHandledPayload>() { Id = Guid.NewGuid(), AuthToken = Guid.NewGuid() };
            var result = DodbGateway.PushDto(dto);
            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoPayloadHandlerNotFound);
        }

        [Test]
        public void T003_SendingDtoWithInvalidDtoProperties()
        {
            var dto = new DtoOf<TestPayload>() { Id = Guid.Empty };
            var result = DodbGateway.PushDto(dto);
            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoIsInvalid);
        }

        [Test]
        public void T004_1_SendingDtoWithAnnotationValidationErrors()
        {
            var dto = new DtoOf<AnnotatedPayload>
            {
                Payload = new()
                {
                    SomeIntValue = -10,
                    SomeStringValue = "Hello world"
                },
                AuthToken = Guid.NewGuid(), // mock, for passing basic validation
                DateCreatedUtc = DateTime.Now // same reason
            };

            var result = DodbGateway.PushDto(dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoIsInvalid);
            Assert.That((result as GrDtoIsInvalid).Errors.Count == 2);
        }

        [Test]
        public void T004_2_SendingDtoWithIValidatableImplementation()
        {
            var dto = new DtoOf<ValidatablePayload>
            {
                Payload = new() { TestValue = 11 },
                AuthToken = Guid.NewGuid(), // mock, for passing basic validation
                DateCreatedUtc = DateTime.Now // same reason
            };

            var result = DodbGateway.PushDto(dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoIsInvalid);
            Assert.That((result as GrDtoIsInvalid).Errors.Count == 1);
        }

        [Test]
        public void T005_SendValidDto()
        {
            var dto = new DtoOf<AnnotatedPayload>()
            {
                Payload = new()
                {
                    SomeIntValue = 0,
                    SomeStringValue = "Anton"
                }
            };

            dto.AuthToken = Guid.NewGuid(); // mock, for passing basic validation

            var result = DodbGateway.PushDto(dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrOk);
        }

        [Test]
        public void T006_SendAnotherValidDto()
        {
            var dto = new DtoOf<AnnotatedPayload>()
            {
                Payload = new()
                {
                    SomeIntValue = 19,
                    SomeStringValue = "Olga"
                }
            };

            dto.AuthToken = Guid.NewGuid(); // mock, for passing basic validation

            var result = DodbGateway.PushDto(dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrOk);
        }

        [Test]
        public void T007_CheckValidationDisableFlag()
        {
            var dto = new DtoOf<AnnotatedPayload>()
            {
                Payload = new()
                {
                    SomeIntValue = 15,
                    SomeStringValue = "SomeO"
                }
            };

            var result = DodbGateway.PushDto(dto, skipAuthTokenValidation: true);

            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrOk);
        }

        [Test]
        public void T008_ValidationOfLists()
        {
            var dto = new DtoOf<ListPayload>();
            var pushResult = DodbGateway.PushDto(dto, skipAuthTokenValidation: true);
            Assert.That(pushResult is GrDtoIsInvalid); // no items added

            // Valid
            dto.Payload.Items.Add(new() { SomeIntValue = 10, SomeStringValue = "12" });
            pushResult = DodbGateway.PushDto(dto, skipAuthTokenValidation: true);
            Assert.That(pushResult is GrOk);

            // Invalid
            dto.Payload.Items.Clear();
            dto.Payload.Items.Add(new() { SomeIntValue = -10, SomeStringValue = "12" }); // -10 is invalid
            pushResult = DodbGateway.PushDto(dto, skipAuthTokenValidation: true);
            Assert.That(pushResult is GrDtoIsInvalid);

            // Invalid + valid = invalid
            dto.Payload.Items.Clear();
            dto.Payload.Items.Add(new() { SomeIntValue = 10, SomeStringValue = "12" }); // valid
            dto.Payload.Items.Add(new() { SomeIntValue = -10, SomeStringValue = "12" }); // valid
            pushResult = DodbGateway.PushDto(dto, skipAuthTokenValidation: true);
            Assert.That(pushResult is GrDtoIsInvalid);
        }

        [Test]
        public void T998_Sync()
        {
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            DodbSyncService.SetSyncDirectoryPath(syncDirectory);
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => File.Delete(f)); // clean up
            DodbSyncService.Sync(); // should create 4 files

            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.AreEqual(4, filesList.Count);
        }

        [Test]
        public void T999_Sync()
        {
            // Make sure we have 4 files from prev. test
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.AreEqual(4, filesList.Count);

            using var dbContext = new TestDbContext();
            dbContext.Documents.RemoveRange(dbContext.Documents);
            dbContext.SaveChanges();
            Assert.AreEqual(false, dbContext.Documents.Any());

            DodbSyncService.Sync();

            Assert.AreEqual(4, dbContext.Documents.Count());
        }
    }
}
