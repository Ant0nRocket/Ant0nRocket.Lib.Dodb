using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responces
{
    public class GrDtoPayloadHandleFailed : GatewayResponse
    {
        public Guid DocumentId { get; init; }

        public override string ToString() =>
            $"Unable to save document '{DocumentId}'";
    }
}
