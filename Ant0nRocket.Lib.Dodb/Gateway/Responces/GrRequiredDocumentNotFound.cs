using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responces
{
    public class GrRequiredDocumentNotFound : GatewayResponse
    {
        public Guid RequesterId { get; init; }

        public Guid RequiredDocumentId { get; init; }

        public override string ToString() =>
            $"DTO '{RequesterId}' requires a document '{RequiredDocumentId}' which is not found";
    }
}
