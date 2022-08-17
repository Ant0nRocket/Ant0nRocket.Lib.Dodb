using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Services.Responces.DodbUsersService
{
    public class GrAuth_Failed : GatewayResponse
    {
        public string UserName { get; init; }
    }
}
