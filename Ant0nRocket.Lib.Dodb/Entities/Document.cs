namespace Ant0nRocket.Lib.Dodb.Entities
{
    public class Document : EntityBase
    {
        public Guid AuthorId { get; set; }

        public Guid RequiredDocumentId { get; set; }

        public string Payload { get; set; }

        public string PayloadType { get; set; }
    }
}
