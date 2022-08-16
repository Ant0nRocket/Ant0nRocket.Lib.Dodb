namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public interface IDodbSyncServicePlugin
    {
        void Sync();

        bool IsReady { get; }

        string Name { get; }
    }
}
