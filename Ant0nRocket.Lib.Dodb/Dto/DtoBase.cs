namespace Ant0nRocket.Lib.Dodb.Dto
{
    /// <summary>
    /// Base of a <see cref="DtoOf{T}"/>. Contains minimum required information.
    /// </summary>
    public abstract class DtoBase
    {
        /// <summary>
        /// Globally unique identifier of a document.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Id of a User that created this DTO
        /// </summary>
        public Guid? UserId { get; set; } = default;

        /// <summary>
        /// Requires specified document to exists in database.
        /// </summary>
        public Guid? RequiredDocumentId { get; set; } = default;

        /// <summary>
        /// Comment of document.
        /// </summary>
        public string? Description { get; set; } = default;

        /// <summary>
        /// Date when the document was created.
        /// </summary>
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
