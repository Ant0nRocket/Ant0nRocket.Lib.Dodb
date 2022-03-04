using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responces
{
    /// <summary>
    /// Returnes when there is some trash object received,
    /// which couldn't be casted to IDto.
    /// </summary>
    public class GrDtoHandlerNotFount : GatewayResponse
    {
        public Dto Value { get; init; }
    }
}
