using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Responses.Attributes;
using Ant0nRocket.Lib.Std20.Reflection;

namespace Ant0nRocket.Lib.Dodb.Extensions
{
    /// <summary>
    /// Extensions for <see cref="IGatewayResponse"/>.
    /// </summary>
    public static class IGatewayResponseExtensions
    {
        /// <summary>
        /// Return false is <see cref="IGatewayResponse"/> marked as <see cref="IsSuccessAttribute.IsSuccess"/>=false.
        /// Othervise (including if there is not IsSuccess attribute) - true.
        /// </summary>
        public static bool IsSuccess(this IGatewayResponse gatewayResponse)
        {
            return AttributeUtils
                .GetAttribute<IsSuccessAttribute>(gatewayResponse.GetType())?.IsSuccess ?? true;
        }
    }
}
