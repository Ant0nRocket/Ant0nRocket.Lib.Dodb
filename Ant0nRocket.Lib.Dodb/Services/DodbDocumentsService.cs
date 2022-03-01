using Ant0nRocket.Lib.Dodb.Abstractions;

namespace Ant0nRocket.Lib.Dodb.Services
{
    public class DodbDocumentsService : IDodbDocumentsService
    {
        public int TestValue { get; } = 10000;

        public IDodbUsersService UsersService { get; init; }

        public DodbDocumentsService(IDodbUsersService usersService)
        {
            this.UsersService = usersService;
        }

        //public static ResponseBase Push<T>(Dto<T> dto) where T : class, new()
        //{
        //    return new R_DocumentCreated();
        //}
    }
}
