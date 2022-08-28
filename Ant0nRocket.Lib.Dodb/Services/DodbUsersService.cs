using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.DtoPayloads;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway;
using Ant0nRocket.Lib.Dodb.Services.Responces.DodbUsersService;
using Ant0nRocket.Lib.Std20.Logging;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Services
{
    /// <summary>
    /// Basic user management service.
    /// </summary>
    public static class DodbUsersService
    {
        private static readonly Logger _logger = Logger.Create(nameof(DodbUsersService));


        #region Public functions

        /// <summary>
        /// Calculates hash of a <paramref name="password"/>.
        /// </summary>
        public static string CalcPasswordHash(string password)
        {
            var _passwordHasherFunc = DodbGateway.GetPasswordHashHandler();
            return _passwordHasherFunc(password);
        }

        /// <summary>
        /// Returnes <see cref="Responces.DodbUsersService.GrAuth_Failed"/> if username or password incorrect.<br />
        /// Returnes <see cref="Responces.DodbUsersService.GrAuth_Success"/> if authentication success.
        /// </summary>
        public static GatewayResponse Auth(string userName, string plainPassword)
        {
            var passwordHash = CalcPasswordHash(plainPassword);
            _logger.LogDebug($"Trying authenticate '{userName}', password hash '{passwordHash}'");

            using var dbContext = DodbGateway.GetDbContext();

            var user = dbContext
                .Users
                .AsNoTracking()
                .SingleOrDefault(u => u.Name == userName && u.PasswordHash == passwordHash);

            if (user == default)
            {
                _logger.LogError($"Auth failed for user '{userName}'");
                return new GrAuth_Failed { UserName = userName };
            }

            _logger.LogInformation($"Auth success for user '{userName}'");
            return new GrAuth_Success { AuthenticatedUser = user };
        }

        /// <summary>
        /// Checks specified by <paramref name="userId"/> user exists.
        /// </summary>
        public static bool CheckUserExists(Guid userId)
        {
            using var dbContext = DodbGateway.GetDbContext();
            return dbContext.Users.AsNoTracking().Any(u => u.Id == userId);
        }

        /// <summary>
        /// Returnes a list of known usernames that is not
        /// deleted and not hidden.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetExistingUserNames()
        {
            using var dbContext = DodbGateway.GetDbContext();
            var users = dbContext.Users.AsNoTracking()
                .Where(u => !u.IsHidden)
                .Select(u => u.Name)
                .ToList();
            return users;
        }

        /// <summary>
        /// Returnes a <see cref="User"/> by <see cref="User.Name"/>.
        /// </summary>
        public static User GetUserByName(string name)
        {
            using var dbContext = DodbGateway.GetDbContext();
            return dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Name == name);
        }

        /// <summary>
        /// Returnes a <see cref="User"/> by <see cref="User.Id"/>.
        /// </summary>
        public static User GetUserById(Guid id)
        {
            using var dbContext = DodbGateway.GetDbContext();
            return dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == id);
        }

        /// <summary>
        /// Returnes users count that is not deleted.
        /// </summary>
        public static int GetUsersCount()
        {
            using var dbContext = DodbGateway.GetDbContext();
            return dbContext.Users.Count();
        }

        /// <summary>
        /// Creates a User described in <paramref name="dtoPayload"/>.<br />
        /// Returnes <see cref="GrCreateUser_Exists"/> if <see cref="User"/> 
        /// with specified Name exists.<br />
        /// Returnes <see cref="GrCreateUser_Success"/> if new User create success.
        /// </summary>
        public static GatewayResponse CreateUser(PldCreateUser dtoPayload, IDodbContext dbContext)
        {
            var user = dbContext
                .Users
                .AsNoTracking()
                .Where(u => u.Name == dtoPayload.Value.Name)
                .FirstOrDefault();

            if (user != default) // user exists
            {
                _logger.LogWarning($"User with a username='{dtoPayload.Value.Name}' already exists");
                return new GrCreateUser_Exists();
            }
            else // user is new
            {
                _logger.LogInformation($"User '{dtoPayload.Value.Name}' created");
                dbContext.Users.Add(dtoPayload.Value);
                return new GrCreateUser_Success();
            }
        }

        #endregion // Internal functions
    }
}
