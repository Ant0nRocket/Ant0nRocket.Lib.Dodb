using Newtonsoft.Json;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ant0nRocket.Lib.Dodb.Models
{
    /// <summary>
    /// Base class for entities.
    /// </summary>
    public abstract partial class EntityBase
    {
        /// <summary>
        /// Id of entity
        /// </summary>
        [Key]
        public virtual Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Id of document that created a record.
        /// </summary>
        [ForeignKey("Document")]
        public virtual Guid DocumentRefId { get; set; }

        /// <summary>
        /// Document that created entity/record.
        /// </summary>
        [JsonIgnore]
        public virtual Document? Document { get; set; }
    }
}
