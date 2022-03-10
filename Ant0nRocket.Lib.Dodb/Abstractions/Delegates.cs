using Ant0nRocket.Lib.Dodb.Dtos;

namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public delegate GatewayResponse ApplyDtoDelegate<TPayload>(TPayload dtoPayload, IDodbContext context);
}
