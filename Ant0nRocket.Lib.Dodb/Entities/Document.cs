namespace Ant0nRocket.Lib.Dodb.Entities
{
    public class Document : EntityBase
    {
        public User User { get; set; }

        public Guid UserId { get; set; }

        public string DtoPayload { get; set; }

        public string DtoType { get; set; }
    }
}
