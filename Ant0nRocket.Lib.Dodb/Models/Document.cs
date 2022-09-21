using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace Ant0nRocket.Lib.Dodb.Models
{
    /// <summary>
    /// Key element of a library. Every operation that changes
    /// a database is a <see cref="Document"/>. Inside of a Document
    /// there are all the data required to reproduce changes that was made.<br />
    /// Additionally, <see cref="Dodb"/> can
    /// export documents to disk and read them back later. This feature
    /// allowes you to sync you data across as many devices as you may need.
    /// </summary>
    public class Document
    {
        /// <summary>
        /// Id of a Document.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Library automatically controls that every Document (except first one)
        /// could be applyed only when required Document exists in database.<br />
        /// Something like block-chain, but without encryption.
        /// </summary>
        public Guid? RequiredDocumentId { get; set; }

        /// <inheritdoc cref="RequiredDocumentId"/>
        [JsonIgnore]
        public Document? RequiredDocument { get; set; }

        /// <summary>
        /// JSON representation of a DTO.
        /// </summary>
        [Required]
        public string? PayloadJson { get; set; }

        /// <summary>
        /// <see cref="Type.FullName"/> of a DTO payload class.
        /// </summary>
        [Required]
        public string? PayloadTypeName { get; set; }

        /// <summary>
        /// Short? annotation about the purpose of a document.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Timestamp (UTC).
        /// </summary>
        [Required]
        public DateTime DateCreatedUtc { get; set; }

        /************************************************************************
         * COLLECTION OF PROPERTIES THAT COULD BE USEFULL IF YOU NEED THEM.
         * IF YOU DON'T WANT - DON'T USE THEM.
         ************************************************************************/

        /// <summary>
        /// Posible Id of an author of the document.
        /// </summary>
        public Guid? UserId { get; set; }
    }
}
