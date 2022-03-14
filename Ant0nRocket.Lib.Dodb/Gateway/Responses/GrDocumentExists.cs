using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    /// <summary>
    /// Returned when <see cref="Lib.Dodb.Entities.Document"/> already exists
    /// in database (by it's Id)
    /// </summary>
    [IsSuccess(false)]
    public class GrDocumentExists : GatewayResponse
    {
        /// <summary>
        /// Id of document that is already exists
        /// </summary>
        public Guid DocumentId { get; init; }

        public override string ToString() =>
            $"Document with Id '{DocumentId}' already exists";
    }
}
