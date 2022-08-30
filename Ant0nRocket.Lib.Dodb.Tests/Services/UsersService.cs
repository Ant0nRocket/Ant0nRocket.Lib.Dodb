using System;

using Ant0nRocket.Lib.Dodb.DbContexts;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Model;
using Ant0nRocket.Lib.Dodb.Tests.Services.Responces.UsersService;
using Ant0nRocket.Lib.Std20.Logging;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Tests.Services
{
    /// <summary>
    /// Basic user management service.
    /// </summary>
    public static class UsersService
    {
        private static readonly Logger _logger = Logger.Create(nameof(UsersService));


        #region Public functions

        /// <summary>
        /// Calculates hash of a <paramref name="password"/>.
        /// </summary>
        public static string CalcPasswordHash(string password)
        {
            throw new NotImplementedException();

            //var _passwordHasherFunc = DodbGateway.GetPasswordHashHandler();
            //return _passwordHasherFunc(password);
        }

        public static IGatewayResponse Auth(string userName, string plainPassword)
        {

            throw new NotImplementedException();

            //var passwordHash = CalcPasswordHash(plainPassword);
            //_logger.LogDebug($"Trying authenticate '{userName}', password hash '{passwordHash}'");

            //using var dbContext = DodbGateway.GetDbContext();

            //var user = dbContext
            //    .Users
            //    .AsNoTracking()
            //    .SingleOrDefault(u => u.Name == userName && u.PasswordHash == passwordHash);

            //if (user == default)
            //{
            //    _logger.LogError($"Auth failed for user '{userName}'");
            //    return new GrAuth_Failed { UserName = userName };
            //}

            //_logger.LogInformation($"Auth success for user '{userName}'");
            //return new GrAuth_Success { AuthenticatedUser = user };
        }

        /// <summary>
        /// Returnes users count that is not deleted.
        /// </summary>
        public static int GetUsersCount()
        {
            return 0;
            //using var dbContext = DodbGateway.GetDbContext();
            //return dbContext.Users.Count();
        }

        /// <summary>
        /// Creates a User described in <paramref name="dtoPayload"/>.<br />
        /// Returnes <see cref="GrCreateUser_Exists"/> if <see cref="User"/> 
        /// with specified Name exists.<br />
        /// Returnes <see cref="GrCreateUser_Success"/> if new User create success.
        /// </summary>
        public static IGatewayResponse CreateUser(PldCreateUser dtoPayload, DodbContextBase dbContext)
        {
            throw new NotImplementedException();

            //var user = dbContext
            //    .Users
            //    .AsNoTracking()
            //    .Where(u => u.Name == dtoPayload.Value.Name)
            //    .FirstOrDefault();

            //if (user != default) // user exists
            //{
            //    _logger.LogWarning($"User with a username='{dtoPayload.Value.Name}' already exists");
            //    return new GrCreateUser_Exists();
            //}
            //else // user is new
            //{
            //    _logger.LogInformation($"User '{dtoPayload.Value.Name}' created");
            //    dtoPayload.Value.DocumentRefId = dtoPayload.GetCarrier().Id;
            //    dbContext.Users.Add(dtoPayload.Value);
            //    return new GrCreateUser_Success();
            //}
        }

        #endregion // Internal functions
    }
}
