using Ant0nRocket.Lib.Dodb.Gateway.Abstractions;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Dodb.Tests.Contexts;
using Ant0nRocket.Lib.Dodb.Tests.Dto.Payloads;
using Ant0nRocket.Lib.Dodb.Tests.Model;

using Microsoft.EntityFrameworkCore;

using System.Linq;

namespace Ant0nRocket.Lib.Dodb.Tests.Services
{
    /// <summary>
    /// Basic user management service.
    /// </summary>
    public static class UsersService
    {
        /// <summary>
        /// Creates a User described in <paramref name="dtoPayload"/>.<br />
        /// Returnes <see cref="GrCreateUser_Exists"/> if <see cref="User"/> 
        /// with specified Name exists.<br />
        /// Returnes <see cref="GrCreateUser_Success"/> if new User create success.
        /// </summary>
        public static IGatewayResponse CreateUser(PldCreateUser dtoPayload, TestDbContext dbContext)
        {
            var user = dbContext
                .Users
                .AsNoTracking()
                .Where(u => u.Name == dtoPayload.Name)
                .FirstOrDefault();

            if (user != default) // user exists
            {
                return new GrDtoPushFailed(dtoPayload, "User name busy");
            }
            else // user is new
            {
                user = new User
                {
                    Name = dtoPayload.Name,
                    PasswordHash = dtoPayload.PasswordHash,
                    IsAdmin = dtoPayload.IsAdmin,
                    IsHidden = dtoPayload.IsHidden,
                    DocumentRefId = dtoPayload.__CarrierId,
                };
                dbContext.Users.Add(user);
                return new GrDtoPushSuccess(dtoPayload);
            }
        }
    }
}
