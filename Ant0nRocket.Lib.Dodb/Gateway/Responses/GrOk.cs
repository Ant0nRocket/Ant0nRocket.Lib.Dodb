using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    [Obsolete("Don't use it in real apps, it's for test purposes only")]
    [IsSuccess(true)]
    public class GrOk : GatewayResponse
    {
    }
}
