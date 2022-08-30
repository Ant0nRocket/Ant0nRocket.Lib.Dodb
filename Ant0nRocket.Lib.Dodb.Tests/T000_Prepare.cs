using Ant0nRocket.Lib.Dodb.DbContexts;
using Ant0nRocket.Lib.Dodb.Dto;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads.Mock;
using Ant0nRocket.Lib.Dodb.Tests.Services;
using Ant0nRocket.Lib.Dodb.Tests.Services.Responces.Mock;
using Ant0nRocket.Lib.Std20;
using Ant0nRocket.Lib.Std20.Logging;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{

    public class T000_Prepare : DodbTestsBase
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

        private IGatewayResponse DtoHandlerMethod(object dtoPayloadObject, DodbContextBase dbContext)
        {
            var c = (TestDbContext)dbContext;
            return dtoPayloadObject switch
            {
                PldCreateUser p => UsersService.CreateUser(p, c),
                AnnotatedPayload => new GrOk(),
                ListPayload => new GrOk(),
                _ => new GrDtoPayloadHandlerNotFound { DtoPayloadTypeName = $"{dtoPayloadObject.GetType()}" }
            };
        }

        [Test]
        public void T002_CheckCarrierFeatures()
        {
            var dto = new DtoOf<PldCreateUser>();

            var pld = dto.Payload;

            Assert.That(pld.GetCarrier() == dto);
        }
    }
}
