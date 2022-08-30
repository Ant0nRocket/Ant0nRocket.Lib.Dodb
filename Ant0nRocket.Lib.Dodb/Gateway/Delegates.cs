using Ant0nRocket.Lib.Dodb.DbContexts;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    public delegate IGatewayResponse DtoPayloadHandler(object dtoPayload, DodbContextBase dbContext);

    public delegate DodbContextBase GetDbContextHandler();

    public delegate string GetPasswordHashHandler(string plainPassword);
}
