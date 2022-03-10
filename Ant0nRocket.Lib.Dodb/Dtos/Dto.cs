using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Dtos
{
    public class Dto
    {
        /// <summary>
        /// Globally unique identifier of a document.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Id of author of the document.
        /// </summary>
        public Guid AuthorId { get; set; } = Guid.Empty;

        /// <summary>
        /// Requires specified document to exists in database.
        /// </summary>
        public Guid RequiredDocumentId { get; set; } = Guid.Empty;

        /// <summary>
        /// Date when the document was created.<br />
        /// Library will assign it automatically.
        /// </summary>
        public DateTime DateCreatedUtc { get; set; } = DateTime.MinValue;
    }
}
