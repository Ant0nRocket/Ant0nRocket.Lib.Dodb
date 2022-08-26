using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Entities
{
    public abstract class EntityBase
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public virtual DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
