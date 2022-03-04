using Ant0nRocket.Lib.Dodb.Dtos;

namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public delegate GatewayResponse ApplyDtoDelegate<T>(DtoOf<T> dto, IDodbContext context) where T : class, new();
}
