
using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Responces;
using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Entities;
using Ant0nRocket.Lib.Dodb.Tests.Services;

using NUnit.Framework;

using System;

namespace Ant0nRocket.Lib.Dodb.Tests
{

    public class T001_Init
    {
        [Test]
        public void T001_RegisterGetterAndHandlers()
        {
            DodbGateway.RegisterContextGetter(new Func<IDodbContext>(() => new TestDbContext()));
            DodbDtoHandler<TestPayload>.RegisterDtoHandler(TestService.TestMethod);
            DodbDtoHandler<AnnotatedPayload>.RegisterDtoHandler((dto, ctx) => new GrDtoSaveSuccess());
        }

        [Test]
        public void T002_SendingUnHandledDtoType()
        {
            var dto = new DtoOf<NotHandledPayload>();
            var result = DodbGateway.PushDto(dto);
            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoHandlerNotFound);
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

            dto.UserId = Guid.NewGuid(); // mock, for passing basic validation
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

            dto.UserId = Guid.NewGuid(); // mock, for passing basic validation
            dto.DateCreatedUtc = DateTime.Now; // same reason

            var result = DodbGateway.PushDto(dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoSaveSuccess);
        }
    }
}
