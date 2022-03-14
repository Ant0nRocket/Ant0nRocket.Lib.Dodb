using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Std20.Logging;

namespace Ant0nRocket.Lib.Dodb.Tests.Services
{
    public static class TestService
    {
        private static readonly Logger logger = Logger.Create(nameof(TestService));

        public static GatewayResponse TestMethod(TestPayload dto, IDodbContext context)
        {
            logger.LogDebug($"some work with TestPayload DTO");
            return new GrOk();
        }

        public static GatewayResponse AnnotatedPayloadMethod(AnnotatedPayload dto, IDodbContext context)
        {
            logger.LogDebug($"some work with Annotated DTO");
            return new GrOk();
        }
    }
}
