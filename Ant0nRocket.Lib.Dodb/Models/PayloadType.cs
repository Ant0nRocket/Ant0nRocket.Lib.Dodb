using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace Ant0nRocket.Lib.Dodb.Models
{
    /// <summary>
    /// Entity for storing a type of a <see cref="Document.PayloadJson"/>.
    /// </summary>
    public class PayloadType
    {
        /// <summary>
        /// Simple Id.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public int Id { get; set; }

        /// <summary>
        /// CLR name of type of a <see cref="Document.PayloadJson"/>.
        /// </summary>
        [Required]
        public string? TypeName { get; set; }
    }
}
