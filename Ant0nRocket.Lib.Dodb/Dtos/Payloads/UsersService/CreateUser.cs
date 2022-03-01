namespace Ant0nRocket.Lib.Dodb.Dtos.Payloads.UsersService
{
    public class CreateUser
    {
        public string Username { get; init; }

        public string Password { get; init; }

        public bool IsAdmin { get; init; }
    }
}
