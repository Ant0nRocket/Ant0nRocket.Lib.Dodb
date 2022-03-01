namespace Ant0nRocket.Lib.Dodb.Entities
{
    public abstract class EntityBase
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
    }
}
