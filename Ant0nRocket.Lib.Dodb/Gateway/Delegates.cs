using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.DbContexts;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    public delegate GatewayResponse DtoPayloadHandler(object dtoPayload, IDodbContext dbContext);

    public delegate DodbContextBase GetDbContextHandler();

    public delegate string GetPasswordHashHandler(string plainPassword);
}
