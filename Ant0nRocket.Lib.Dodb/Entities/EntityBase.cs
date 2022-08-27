using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Entities
{
    /// <summary>
    /// Base class for entyties of Dodb database.
    /// You could use, or not - it doesn't matter.
    /// </summary>
    public abstract class EntityBase
    {
        /// <summary>
        /// Globally unique Id.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();


        /// <summary>
        /// Timestamp.
        /// </summary>
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
