namespace Ant0nRocket.Lib.Dodb.Dtos
{
    public class Dto<T> where T : class, new()
    {
        /// <summary>
        /// Globally unique identifier of a document.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Id of author of the document.
        /// </summary>
        public Guid UserId { get; set; } = Guid.Empty;

        /// <summary>
        /// Requires specified document to exists in database.
        /// </summary>
        public Guid RequiredDocumentId { get; set; } = Guid.Empty;

        /// <summary>
        /// Date when the document was created.<br />
        /// Library will assign it automatically.
        /// </summary>
        public DateTime DateCreated { get; set; } = DateTime.MinValue;

        /// <summary>
        /// A payload of the document. Everything could be here,
        /// no limitations.
        /// </summary>
        public T Payload { get; set; } = new();
    }
}
