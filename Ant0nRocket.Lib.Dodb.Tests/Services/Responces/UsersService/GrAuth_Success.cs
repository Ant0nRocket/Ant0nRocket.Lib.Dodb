using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Tests.Model;

namespace Ant0nRocket.Lib.Dodb.Tests.Services.Responces.UsersService
{
    public class GrAuth_Success : IGatewayResponse
    {
        public User AuthenticatedUser { get; init; }
    }
}
