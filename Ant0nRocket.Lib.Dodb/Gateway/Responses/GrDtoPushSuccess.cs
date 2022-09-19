using Ant0nRocket.Lib.Dodb.Dto;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    /// <summary>
    /// <see cref="Dodb"/> will return this when push success.
    /// </summary>
    public class GrDtoPushSuccess : IGatewayResponse
    {
        /// <summary>
        /// Instance of DTO that were applied.
        /// </summary>
        public DtoBase? Dto { get; set; }
    }
}
