namespace Ant0nRocket.Lib.Dodb.Entities
{
    public class User : EntityBase
    {
        public string Name { get; set; }

        public string PasswordHash { get; set; }

        public bool IsAdmin { get; set; } = false;

        public bool IsHidden { get; set; } = false;
    }
}
