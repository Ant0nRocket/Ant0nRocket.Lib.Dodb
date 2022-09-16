﻿using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Models
{
    /// <summary>
    /// Payload of a Document.
    /// </summary>
    public class DocumentPayload : EntityBase
    {
        /// <summary>
        /// <see cref="Type.FullName"/> of a DTO payload class.
        /// </summary>
        [Required]
        public virtual string? PayloadTypeName { get; set; }

        /// <summary>
        /// JSON representation of a DTO.
        /// </summary>
        [Required]
        public virtual string? PayloadJson { get; set; }
    }
}
