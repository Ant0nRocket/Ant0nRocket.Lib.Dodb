﻿using System;

using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.DtoPayloads;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Services;
using Ant0nRocket.Lib.Dodb.Services.Responces.DodbUsersService;
using Ant0nRocket.Lib.Std20.Testing;

using NUnit.Framework;

namespace Ant0nRocket.Lib.Dodb.Tests
{
    internal class T001_DodbUsersServiceTests : TestBase
    {
        private static User? rootUser;

        private GatewayResponse CreateUser(
            Guid documentAuthorId,
            string userName,
            string plainPassword,
            bool isAdmin = false,
            bool isHidden = false,
            bool sameUserIdInDtoAndPayload = false)
        {
            var dto = new DtoOf<PldCreateUser>();
            dto.UserId = documentAuthorId;
            dto.Payload.Value.Name = userName;
            dto.Payload.Value.PasswordHash = DodbUsersService.CalcPasswordHash(plainPassword);
            dto.Payload.Value.IsAdmin = isAdmin;
            dto.Payload.Value.IsHidden = isHidden;

            if (sameUserIdInDtoAndPayload)
                dto.UserId = dto.Payload.Value.Id;

            return DodbGateway.PushDto(dto);
        }

        public static void AuthUser(string userName, string plainPassword, out User? result)
        {
            var authResult = DodbUsersService.Auth(userName, plainPassword);
            if (authResult is GrAuth_Success sResult)
                result = sResult.AuthenticatedUser;
            else
                result = default;
        }

        [Test]
        public void T001_CreateAndAuthRootUser()
        {
            LogStart();

            var createResult = CreateUser(default, "root", "root", isAdmin: true, isHidden: true, sameUserIdInDtoAndPayload: true);
            Assert.That(createResult is GrCreateUser_Success);
            AuthUser("root", "root", out rootUser);
            Assert.That(rootUser is not null);

            LogEnd();
        }

        [Test]
        public void T002_CheckUsersCount()
        {
            var usersCount = DodbUsersService.GetUsersCount();
            Assert.AreEqual(1, usersCount);
        }

        [Test]
        public void T004_TryCreateExistingUser()
        {
            var createResult = CreateUser(rootUser?.Id ?? default, "root", "root");
            Assert.That(createResult is GrCreateUser_Exists);
        }

        [Test]
        public void T005_TryAuthWithWrongCredentials()
        {
            Assert.That(DodbUsersService.Auth("UserX", "with strange password") is GrAuth_Failed);
        }
    }
}
