using Ant0nRocket.Lib.Std20.Logging;

using System.Runtime.CompilerServices;

namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public abstract class GatewayResponse
    {
        public DateTime DateCreatedUtc = DateTime.UtcNow;

        public GatewayResponse WithLogging([CallerMemberName] string methodName = default, LogLevel logLevel = LogLevel.Info)
        {
            Logger.Log(this.ToString(), logLevel, methodName);
            return this;
        }
    }
}
