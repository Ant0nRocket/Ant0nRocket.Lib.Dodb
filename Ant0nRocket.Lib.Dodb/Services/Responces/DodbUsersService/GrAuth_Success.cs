using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Entities;

namespace Ant0nRocket.Lib.Dodb.Services.Responces.DodbUsersService
{
    public class GrAuth_Success : GatewayResponse
    {
        public User AuthenticatedUser { get; init; }
    }
}
