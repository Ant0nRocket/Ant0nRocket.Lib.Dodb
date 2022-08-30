using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Extensions;
using Ant0nRocket.Lib.Dodb.Tests.Services.Responces.UsersService;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    internal class T002_DodbUsersService : DodbTestsBase
    {

        [Test]
        public void T001_CreateUser()
        {
            var dto = CreateDto<PldCreateUser>();
            dto.Payload.Name = "Dodb";
            dto.Payload.PasswordHash = "some hash";
            var pushResult = Push(dto);

            pushResult.AssertIs<GrCreateUser_Success>();
        }
    }
}
