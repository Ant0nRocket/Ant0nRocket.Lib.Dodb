
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

            DodbGateway.RegisterContextGetter(new Func<IDodbContext>(() => new TestDbContext()));
            DodbGateway.RegisterDtoHandler(DtoHandlerMethod);

        }

        private GatewayResponse DtoHandlerMethod(object dtoPayloadObject, IDodbContext dbContext)
        {
            return dtoPayloadObject switch
            {
                TestPayload dtoPayload => TestService.TestMethod(dtoPayload, dbContext),
                AnnotatedPayload p => new GrOk(),
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
        public void T004_SendingDtoWithAnnotationValidationErrors()
        {
            var dto = new DtoOf<AnnotatedPayload>() { 
                Payload = new() 
                { 
                    SomeIntValue = -10, 
                    SomeStringValue = "Hello world" 
                } 
            };

            dto.AuthToken = Guid.NewGuid(); // mock, for passing basic validation
            dto.DateCreatedUtc = DateTime.Now; // same reason

            var result = DodbGateway.PushDto(dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoIsInvalid);
            Assert.That((result as GrDtoIsInvalid).Errors.Count == 2);
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
        public void T008_Sync()
        {
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            DodbSyncService.SetSyncDirectoryPath(syncDirectory);
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => File.Delete(f)); // clean up
            DodbSyncService.Sync(); // should create 3 files

            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.AreEqual(3, filesList.Count);
        }

        [Test]
        public void T009_Sync()
        {
            // Make sure we have 3 files from prev. test
            var syncDirectory = Path.Combine(FileSystemUtils.GetDefaultAppDataFolderPath(), "Sync");
            var filesList = new List<string>();
            FileSystemUtils.ScanDirectoryRecursively(syncDirectory, f => filesList.Add(f));
            Assert.AreEqual(3, filesList.Count);

            using var dbContext = new TestDbContext();
            dbContext.Documents.RemoveRange(dbContext.Documents);
            dbContext.SaveChanges();
            Assert.AreEqual(false, dbContext.Documents.Any());

            DodbSyncService.Sync();

            Assert.AreEqual(3, dbContext.Documents.Count());
        }
    }
}
