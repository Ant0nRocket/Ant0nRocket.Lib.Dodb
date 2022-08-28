using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    /// <summary>
    /// Response for situations when DTO must have RequiredDocumentId but dont have it.
    /// </summary>
    public class GrRequiredDocumentNotSpecified : GatewayResponse
    {
        /// <summary>
        /// Id of invalid DTO.
        /// </summary>
        public Guid DtoId { get; set; } = Guid.Empty;
    }
}
