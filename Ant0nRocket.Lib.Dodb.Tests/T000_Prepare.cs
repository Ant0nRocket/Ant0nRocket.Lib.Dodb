using System;

using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.DtoPayloads;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Entities;
using Ant0nRocket.Lib.Dodb.Tests.Services;
using Ant0nRocket.Lib.Std20;
using Ant0nRocket.Lib.Std20.Logging;
using Ant0nRocket.Lib.Std20.Testing;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{

    public class T000_Prepare : TestBase
    {
        [Test]
        public void T001_RegisterGetterAndHandlers()
        {
            Ant0nRocketLibConfig.IsPortableMode = true;
            BasicLogWritter.LogFileNamePrefix = "DodbGateway.Tests_";
            Logger.LogToBasicLogWritter = true;

            DodbGateway.Initialize(
                getDbContextHandler: () => new TestDbContext(),
                dtoPayloadHandler: DtoHandlerMethod);
        }

        private GatewayResponse DtoHandlerMethod(object dtoPayloadObject, IDodbContext dbContext)
        {
            return dtoPayloadObject switch
            {
                TestPayload dtoPayload => TestService.TestMethod(dtoPayload, dbContext),
                AnnotatedPayload => new GrOk(),
                ListPayload => new GrOk(),
                _ => new GrDtoPayloadHandlerNotFound()
            };
        }

        [Test]
        public void T002_CheckCarrierFeatures()
        {
            var dto = new DtoOf<TestPayload>();

            var pld = dto.Payload;

            Assert.That(pld.GetCarrier() == dto);
        }
    }
}
