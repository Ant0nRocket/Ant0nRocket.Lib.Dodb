namespace Ant0nRocket.Lib.Dodb.Entities
{
    public class User : EntityBase
    {
        public string Login { get; set; }

        public string PasswordHash { get; set; }
    }
}
