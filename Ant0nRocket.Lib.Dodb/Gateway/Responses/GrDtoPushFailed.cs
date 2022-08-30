using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Responses.Attributes;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    [IsSuccess(false)]
    public class GrDtoPushFailed : IGatewayResponse
    {
        public string Message { get; set; }
    }
}
