namespace Ant0nRocket.Lib.Dodb.Gateway.Responces
{
    /// <summary>
    /// Returned when <see cref="Lib.Dodb.Entities.Document"/> already exists
    /// in database (by it's Id)
    /// </summary>
    public class GrDocumentExists
    {
        /// <summary>
        /// Id of document that is already exists
        /// </summary>
        public Guid DocumentId { get; init; }
    }
}
