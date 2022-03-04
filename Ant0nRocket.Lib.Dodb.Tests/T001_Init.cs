
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
            DodbGateway.DtoHandleMap.Add(typeof(TestPayload),)
            //DodbGateway.RegisterDtoHandler<TestPayload>(obj => TestService.TestMethod(obj));
        }

        [Test]
        public void T002_SendingUnHandledDtoType()
        {
            var dto = new DtoOf<NotHandledPayloadType>();
            var result = DodbGateway.PushDto(dto);
            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoHandlerNotFount);
        }

        [Test]
        public void T003_SendingDtoWithInvalidIDtoProperties()
        {
            var dto = new DtoOf<TestPayload>() { Id = Guid.Empty };
            var result = DodbGateway.PushDto(dto);
            Assert.That(result, Is.Not.Null);
            Assert.That(result is GrDtoIsInvalid);
        }
    }
}
