namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public interface IDto
    {
        Guid Id { get; set; }

        DateTime DateCreated { get; set; }

        Guid UserId { get; set; }

        Guid RequiredDocumentId { get; set; }

    }
}