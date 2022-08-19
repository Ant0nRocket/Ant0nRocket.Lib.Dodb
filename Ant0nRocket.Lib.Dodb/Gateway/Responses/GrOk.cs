using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;

namespace Ant0nRocket.Lib.Dodb.Gateway.Responses
{
    /// <summary>
    /// Class for test purposes only. Avoid using it in real
    /// applications. Simple OK means nothing.
    /// </summary>
    [IsSuccess(true)]
    public class GrOk : GatewayResponse
    {
    }
}
