using System.Threading.Tasks;

using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Extensions;
using Ant0nRocket.Lib.Dodb.Tests.Services.Responces.UsersService;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    internal class T002_DodbUsersService : DodbTestsBase
    {

        [Test]
        public async Task T001_CreateUser()
        {
            var dto = CreateDto<PldCreateUser>();
            dto.Payload.Name = "Dodb";
            dto.Payload.PasswordHash = "some hash";
            var pushResult = await Dodb.PushDtoAsync(dto);

            pushResult.AssertIs<GrCreateUser_Success>();
        }
    }
}
