using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace Ant0nRocket.Lib.Dodb.Entities
{
    /// <summary>
    /// Key element of a library. Every operation that changes
    /// a database is a <see cref="Document"/>. Inside of a Document
    /// there are all the data required to reproduce changes that was made.<br />
    /// Additionally, <see cref="Dodb.Gateway.DodbSyncService"/> can
    /// export documents to disk and read them back later. This feature
    /// allowes you to sync you data across as many devices as you may need.
    /// </summary>
    public class Document : EntityBase
    {
        /// <summary>
        /// Id of a <see cref="Entities.User"/> that created this Document.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Instance of the <see cref="Entities.User"/> that created this Document.
        /// </summary>
        [JsonIgnore]
        public User? User { get; set; }

        /// <summary>
        /// Library automatically controls that every Document (except first one)
        /// could be applyed only when required Document exists in database.<br />
        /// Something like block-chain, but without encryption.
        /// </summary>
        public Guid RequiredDocumentId { get; set; }

        /// <summary>
        /// A payload of a DTO, that created this document, serialized as JSON.
        /// </summary>
        [Required]
        public string? PayloadJson { get; set; }

        /// <inheritdoc cref="PayloadType.Id"/>
        [JsonIgnore]
        public int PayloadTypeId { get; set; }


        /// <inheritdoc cref="Entities.PayloadType"/>
        public PayloadType? PayloadType { get; set; }

        /// <summary>
        /// Comment from user.
        /// </summary>
        public string? Description { get; set; }
    }
}
