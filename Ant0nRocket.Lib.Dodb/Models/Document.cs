using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace Ant0nRocket.Lib.Dodb.Models
{
    /// <summary>
    /// Key element of a library. Every operation that changes
    /// a database is a <see cref="Document"/>. Inside of a Document
    /// there are all the data required to reproduce changes that was made.<br />
    /// Additionally, <see cref="Gateway.DodbGateway"/> can
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
        /// <b>ATTENSION!</b><br />
        /// It's obvious that every document has it's author. But
        /// during development of this library I can't be shure in which
        /// table you prefer to store users. How do you call them? Etc.<br />
        /// Please, fill free to populate this field with any Id you want.<br />
        /// But don't forget to set foreign keys in your DbContext!<br />
        /// Of course, validation is on your side.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// A payload of a DTO, that created this document, serialized as JSON.
        /// </summary>
        [Required]
        public string? PayloadJson { get; set; }

        /// <inheritdoc cref="PayloadType.Id"/>
        [JsonIgnore]
        public int PayloadTypeId { get; set; }

        /// <summary>
        /// One-to-one relation to type of payload.
        /// </summary>
        public PayloadType? PayloadType { get; set; }

        /// <summary>
        /// Timestamp (UTC).
        /// </summary>
        [Required]
        public DateTime DateCreatedUtc { get; set; }
    }
}
