using System;

using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Model;
using Ant0nRocket.Lib.Dodb.Tests.Services;
using Ant0nRocket.Lib.Dodb.Tests.Services.Responces.UsersService;

using Ant0nRocket.Lib.Dodb.Tests.Extensions;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    internal class T001_DodbUsersServiceTests : DodbTestsBase
    {
        private static User? rootUser;

        [Test]
        public void T001_CreateAndAuthRootUser()
        {
            var dto = CreateDto<PldCreateUser>();
            var pushResult = Push(dto);

            pushResult.AssertIs<GrCreateUser_Success>();

            var authResult = UsersService.Auth("__root", "root");
            authResult.AssertIs<GrAuth_Success>();
        }

        [Test]
        public void T002_CheckUsersCount()
        {
            var usersCount = UsersService.GetUsersCount();
            Assert.AreEqual(1, usersCount);
        }

        [Test]
        public void T004_TryCreateExistingUser()
        {
            //var createResult = CreateUser(rootUser?.Id ?? default, "__root", "root");
            //Assert.That(createResult is GrCreateUser_Exists);
        }

        [Test]
        public void T005_TryAuthWithWrongCredentials()
        {
            Assert.That(UsersService.Auth("UserX", "with strange password") is GrAuth_Failed);
        }
    }
}
