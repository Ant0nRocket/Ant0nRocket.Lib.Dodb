using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Tests.Plugins
{
    internal class SyncPluginTest : IDodbSyncServicePlugin
    {
        public bool IsReady => true;

        public string Name => nameof(SyncPluginTest);

        public void Sync()
        {
            
        }
    }
}
