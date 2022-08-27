namespace Ant0nRocket.Lib.Dodb.Dtos
{
    /// <summary>
    /// Base of a <see cref="DtoOf{T}"/>. Contains minimum required information.
    /// </summary>
    public class Dto
    {
        /// <summary>
        /// Globally unique identifier of a document.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Id of a <see cref="Entities.User"/> that created this DTO
        /// </summary>
        public Guid UserId { get; set; } = Guid.Empty;

        /// <summary>
        /// Requires specified document to exists in database.
        /// </summary>
        public Guid RequiredDocumentId { get; set; } = Guid.Empty;

        /// <summary>
        /// Comment of document.
        /// </summary>
        public string? Description { get; set; } = default;

        /// <summary>
        /// Date when the document was created.<br />
        /// Library will assign it automatically.
        /// </summary>
        public DateTime DateCreatedUtc { get; set; } = DateTime.MinValue;
    }
}
