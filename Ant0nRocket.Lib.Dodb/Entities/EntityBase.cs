using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ant0nRocket.Lib.Dodb.Entities
{
    /// <summary>
    /// Base class for entities.
    /// </summary>
    public abstract class EntityBase
    {
        /// <summary>
        /// Id of entity
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Id of document that created a record.
        /// </summary>
        [ForeignKey("Document")]
        public Guid DocumentRefId { get; set; }
        
        /// <summary>
        /// Document that created entity/record.
        /// </summary>
        public Document? Document { get; set; }
    }
}
