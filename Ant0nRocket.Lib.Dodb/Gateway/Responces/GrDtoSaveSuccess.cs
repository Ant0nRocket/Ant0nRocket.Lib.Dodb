using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responces
{
    public class GrDtoSaveSuccess : GatewayResponse
    {
        public Guid DocumentId { get; init; }

        public override string ToString() =>
            $"Document '{DocumentId}' successfully saved";
    }
}
