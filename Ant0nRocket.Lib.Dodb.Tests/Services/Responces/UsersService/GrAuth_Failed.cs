using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Tests.Services.Responces.UsersService
{
    public class GrAuth_Failed : IGatewayResponse
    {
        public string UserName { get; init; }
    }
}
