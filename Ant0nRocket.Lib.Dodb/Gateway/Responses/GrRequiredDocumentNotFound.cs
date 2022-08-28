using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    [IsSuccess(false)]
    public class GrRequiredDocumentNotFound : GatewayResponse
    {
        public Guid RequesterId { get; init; }

        public Guid? RequiredDocumentId { get; init; }
    }
}
