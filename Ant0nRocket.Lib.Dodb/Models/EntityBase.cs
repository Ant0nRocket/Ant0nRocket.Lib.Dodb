using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace Ant0nRocket.Lib.Dodb.Models
{
    /// <summary>
    /// Base class for entities.
    /// </summary>
    public abstract class EntityBase : ObservableObject
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
