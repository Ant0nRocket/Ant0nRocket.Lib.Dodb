namespace Ant0nRocket.Lib.Dodb.Entities
{
    public abstract class EntityBase
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
    }
}
