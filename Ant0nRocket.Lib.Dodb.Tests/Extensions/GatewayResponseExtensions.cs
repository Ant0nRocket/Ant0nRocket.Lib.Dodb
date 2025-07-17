using System;

using Ant0nRocket.Lib.Dodb.Enums;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests.Extensions
{
    internal static class GatewayResponseExtensions
    {
        public static T AssertIs<T>(this IGatewayResponse gatewayResponse)
        {
            Assert.That(gatewayResponse is not null);
            Assert.That(typeof(T) == gatewayResponse?.GetType());
            if (gatewayResponse is T result) return result;
            throw new ApplicationException("imposible");
        }

        public static void AssertFailReasonIs(this IGatewayResponse gatewayResponse, GrPushFailReason reason)
        {
            Assert.That(gatewayResponse is not null);
            if (gatewayResponse is GrDtoPushFailed f)
            {
                Assert.That(reason == f.Reason);
            }
            else
            {
                Assert.Fail("Non GrDtoPushFailed received");
            }
        }
    }
}
