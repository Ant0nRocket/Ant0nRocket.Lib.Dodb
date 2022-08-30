using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Responses.Attributes;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    [IsSuccess(false)]
    public class GrDtoRequiredDocumentNotFound : IGatewayResponse
    {
        public Guid RequesterId { get; init; }

        public Guid? RequiredDocumentId { get; init; }
    }
}
