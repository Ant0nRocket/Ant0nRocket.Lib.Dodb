using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Gateway.Responces;

namespace Ant0nRocket.Lib.Dodb.Tests.Services
{
    public static class TestService
    {
        public static GatewayResponse TestMethod(Dto<TestPayload> dto)
        {
            return new GrDocumentSaved();
        }
    }
}
