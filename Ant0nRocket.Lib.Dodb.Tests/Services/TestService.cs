using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Gateway.Responces;
using Ant0nRocket.Lib.Std20.Logging;

namespace Ant0nRocket.Lib.Dodb.Tests.Services
{
    public static class TestService
    {
        private static readonly Logger logger = Logger.Create(nameof(TestService));

        public static GatewayResponse TestMethod(Dto<TestPayload> dto)
        {
            logger.LogDebug($"some work with TestPayload DTO");
            return new GrDocumentSaved();
        }
    }
}
