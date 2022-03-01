namespace Ant0nRocket.Lib.Dodb.Entities
{
    public class Document : EntityBase
    {
        public Guid AuthorId { get; set; }

        public string DtoPayload { get; set; }

        public string DtoType { get; set; }
    }
}
