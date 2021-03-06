namespace Ant0nRocket.Lib.Dodb.Dtos
{
    public class Dto
    {
        /// <summary>
        /// Globally unique identifier of a document.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Authorization token. If you don't want the gateway
        /// check this field Push with <i>skipAuthTokenValidation</i> set to true.
        /// </summary>
        public Guid AuthToken { get; set; } = Guid.Empty;

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
