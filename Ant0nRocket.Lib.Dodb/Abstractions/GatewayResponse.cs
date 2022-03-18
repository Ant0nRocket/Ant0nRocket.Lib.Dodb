namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public abstract class GatewayResponse
    {
        public DateTime DateCreatedUtc = DateTime.UtcNow;

        public virtual string Message { get; set; } = string.Empty;
    }
}
