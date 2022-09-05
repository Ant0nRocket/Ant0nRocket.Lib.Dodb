using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Models
{
    public class DocumentPayload : EntityBase
    {
        [Required]
        public string? PayloadTypeName { get; set; }

        [Required]
        public string? PayloadJson { get; set; }
    }
}
