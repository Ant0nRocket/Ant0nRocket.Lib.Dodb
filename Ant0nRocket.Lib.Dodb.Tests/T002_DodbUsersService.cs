using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Extensions;

using NUnit.Framework;

using System.Threading.Tasks;

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

            pushResult.AssertIs<GrDtoPushSuccess>();
        }
    }
}
