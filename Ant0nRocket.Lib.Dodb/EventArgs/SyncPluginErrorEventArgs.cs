using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.EventArgs
{
    public class SyncPluginErrorEventArgs
    {
        public IDodbSyncServicePlugin Plugin { get; set; }

        public Exception Exception { get; set; }
    }
}
