using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Models
{
    /// <summary>
    /// Payload of a Document.
    /// </summary>
    public class DocumentPayload : EntityBase
    {      
        /// <summary>
        /// JSON representation of a DTO.
        /// </summary>
        [Required]
        public string? PayloadJson { get; set; }
    }
}
