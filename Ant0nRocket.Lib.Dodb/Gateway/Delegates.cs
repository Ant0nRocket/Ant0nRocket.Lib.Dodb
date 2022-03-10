using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    public delegate GatewayResponse DtoHandler(DtoOf<object> dto, IDodbContext dbContext);
}
