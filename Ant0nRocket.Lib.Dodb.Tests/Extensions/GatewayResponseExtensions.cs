using System;

using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests.Extensions
{
    internal static class GatewayResponseExtensions
    {
        public static void AssertIs<T>(this IGatewayResponse gatewayResponse)
        {
            Assert.AreEqual(typeof(T), gatewayResponse.GetType());
        }
    }
}
