using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Responses.Attributes;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    /// <summary>
    /// Returned when <see cref="Models.Document"/> already exists
    /// in database (by it's Id)
    /// </summary>
    [IsSuccess(false)]
    public class GrDtoDocumentExists : IGatewayResponse
    {
        /// <summary>
        /// Id of document that is already exists
        /// </summary>
        public Guid DocumentId { get; init; }
    }
}
