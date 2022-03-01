using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Dtos.Payloads.UsersService;

namespace Ant0nRocket.Lib.Dodb.Abstractions
{
    public interface IDodbUsersService
    {
        IDodbServiceResponse CreateUser(Dto<CreateUser> dto);

        IDodbServiceResponse UpdateUser(Dto<UpdateUser> dto);

        IDodbServiceResponse DeleteUser(Dto<DeleteUser> dto);
    }
}
