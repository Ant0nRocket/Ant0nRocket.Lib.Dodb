using Ant0nRocket.Lib.Dodb.Dto;
using Ant0nRocket.Lib.Dodb.Dto.Payloads.Abstractions;
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
        public DtoBase? Dto { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dto">DTO of type <see cref="DtoBase"/></param>
        public GrDtoPushSuccess(DtoBase? dto)
        {
            Dto = dto;
        }   

        /// <summary>
        /// Constructor which consumes a DTO payload.
        /// </summary>
        public GrDtoPushSuccess(IPayload payload)
        {
            Dto = payload?.GetCarrier();
        }
    }
}
