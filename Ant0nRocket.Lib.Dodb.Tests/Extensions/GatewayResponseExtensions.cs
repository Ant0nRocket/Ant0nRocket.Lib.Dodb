using System;

using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests.Extensions
{
    internal static class GatewayResponseExtensions
    {
        public static T AssertIs<T>(this IGatewayResponse gatewayResponse)
        {
            Assert.NotNull(gatewayResponse);
            Assert.AreEqual(typeof(T), gatewayResponse.GetType());
            if (gatewayResponse is T result) return result;
            throw new ApplicationException("imposible");
        }
    }
}
