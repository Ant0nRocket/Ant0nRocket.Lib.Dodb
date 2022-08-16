using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway.Helpers;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Std20.Cryptography;
using Ant0nRocket.Lib.Std20.Extensions;
using Ant0nRocket.Lib.Std20.Logging;
using Ant0nRocket.Lib.Std20.Reflection;

using Microsoft.EntityFrameworkCore;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    /// <summary>
    /// A gateway for document-oriented database.<br />
    /// </summary>
    public static class DodbGateway
    {
        #region Init

        private static readonly Logger logger = Logger.Create(nameof(DodbGateway));

        #endregion

        #region DbContext getter

        /*
         Why we need a DbContext getter?
         This class doesn't know about the final class you will work with but it has to be IDodbContext.
         As you DbContext will be somewhere in external library we need to know how to get it.
         */

        private static Func<IDodbContext> dbContextGetterFunc;

        public static void RegisterDbContextGetterFunc(Func<IDodbContext> value) =>
            dbContextGetterFunc = value;

        public static IDodbContext GetDbContext() =>
            dbContextGetterFunc?.Invoke();

        #endregion

        #region DTO handler

        private static DtoPayloadHandler dtoPayloadHandler;

        public static void RegisterDtoHandler(DtoPayloadHandler handler) => dtoPayloadHandler = handler;

        #endregion

        private static GatewayResponse PushDtoObject<TPayload>(DtoOf<TPayload> dto, IDodbContext dbContext) where TPayload : class, new()
        {
            #region 1. Checking handlers and values

            if (dbContext == default) throw new ArgumentNullException(nameof(dbContext));
            if (dbContextGetterFunc == default) throw new NullReferenceException(nameof(dbContextGetterFunc));
            if (dtoPayloadHandler == default) throw new NullReferenceException(nameof(dtoPayloadHandler));

            #endregion

            #region 2. Check document doesn't exists, check required document exists

            if (dbContext.Documents.Any(d => d.Id == dto.Id))
                return new GrDocumentExists { DocumentId = dto.Id };

            if (dto.RequiredDocumentId != default) // some document ID required
                if (!dbContext.Documents.AsNoTracking().Any(d => d.Id == dto.RequiredDocumentId))
                    return new GrRequiredDocumentNotFound
                    { // if required document is not found - end. Maybe next time it will be there.
                        RequesterId = dto.Id,
                        RequiredDocumentId = dto.RequiredDocumentId
                    };

            #endregion

            #region Handling DTO, saving document

            var dtoHandleResponse = dtoPayloadHandler(dto.Payload, dbContext);

            var isDtoHandledSuccessfully = AttributeUtils
                .GetAttribute<IsSuccessAttribute>(dtoHandleResponse.GetType())?.IsSuccess ?? true;

            if (isDtoHandledSuccessfully == false)
            {
                logger.LogError($"Got {dtoHandleResponse.GetType().Name} for DTO '{dto.Id}': " +
                    $"{dtoHandleResponse.AsJson()}");
                return dtoHandleResponse;
            }


            if (dto.DateCreatedUtc == DateTime.MinValue) // means that DTO created in app
            {                                            // because deserialized has some value
                dto.RequiredDocumentId = dbContext
                    .Documents
                    .AsNoTracking()
                    .OrderByDescending(d => d.DateCreatedUtc)
                    .FirstOrDefault()?.Id ?? Guid.Empty; // As we have a new DTO let's get latest document ID
                dto.DateCreatedUtc = DateTime.UtcNow;
            }

            var document = new Document
            {
                Id = dto.Id,
                UserId = dto.UserId,
                RequiredDocumentId = dto.RequiredDocumentId,
                DateCreatedUtc = dto.DateCreatedUtc,
                PayloadType = $"{dto.Payload.GetType()}",
                Payload = dto.Payload.AsJson()
            };

            dbContext.Documents.Add(document);

            logger.LogInformation($"[uncommited] DTO '{dto.Id}' applied");

            #endregion

            return dtoHandleResponse;
        }

        /// <summary>
        /// 1. Throws <see cref="NullReferenceException"/> if you forgot to register ContextGetter (see <see cref="RegisterDbContextGetterFunc(Func{IDodbContext})"/>).<br /> 
        /// 2. Throws <see cref="NullReferenceException"/> if you forgot to register DtoHandler (see <see cref="RegisterDtoHandler(DtoPayloadHandler)"/>).<br />
        /// 3. Returnes <see cref="GrDtoIsInvalid"/> if there are some validation errors in DTO or its payload.<br />
        /// 4. Returnes <see cref="GrDocumentExists"/> if any document with <paramref name="dto"/>.Id already exists.<br />
        /// 5. Returnes <see cref="GrRequiredDocumentNotFound"/> if there is some document required to exist but not found.<br />
        /// 6. Returnes <see cref="GrDtoPayloadHandlerNotFound"/> if there is no handler found for payload.<br />
        /// 7. Returnes <see cref="GrPushDtoFailed"/> if there some errors durring commit.<br />
        /// <br />
        /// Othervise returnes some <see cref="GatewayResponse"/><br />
        /// ------------------------<br />
        /// If need to prevent authToken validation (say, in SyncService) set <paramref name="skipAuthTokenValidation"/>
        /// to true.<br />
        /// If <paramref name="externalDbContext"/> passed then all transaction control, saving, disposing - is not 
        /// a business of current function. If you need just push DTO and commit it - dont set <paramref name="externalDbContext"/>!
        /// </summary>
        public static GatewayResponse PushDto<TPayload>(
            DtoOf<TPayload> dto,
            IDodbContext externalDbContext = default
        ) where TPayload : class, new()
        {
            var validator = new DtoValidator<TPayload>(dto).Validate();
            if (validator.HasFoundErrors) return new GrDtoIsInvalid(validator.ErrorsList);

            var dbContext = externalDbContext ?? GetDbContext();
            var pushResult = PushDtoObject(dto, dbContext);

            if (externalDbContext == default)
            {
                try
                {
                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    var message = $"{ex.Message} " + ex.InnerException?.Message ?? string.Empty;
                    pushResult = new GrPushDtoFailed { Message = message };
                    logger.LogException(ex, $"Unable to proceed DTO '{dto.Id}'");
                }
                finally
                {
                    dbContext.Dispose();
                }
            }

            return pushResult;
        }

        #region Root user

        private static Func<string, string> passwordHasherFunc;

        public static void RegisterPasswordHasherFunc(Func<string, string> value) =>
            passwordHasherFunc = value;

        /// <summary>
        /// Will calculate password hash.<br />
        /// If password hasher registred (<see cref="RegisterPasswordHasherFunc(Func{string, string})"/>
        /// then it will be used, othervise standard SHA-256 hash will be returned.
        /// </summary>
        public static string CalcPasswordHash(string password)
        {
            if (passwordHasherFunc != null)
            {
                return passwordHasherFunc(password);
            }
            else
            {
                return Hasher.CalculateHash(password);
            }
        }

        public static User CreateRootUser(string password = "")
        {
            using var dbContext = GetDbContext();
            if (dbContext.Users.Any(u => u.Name == "root"))
            {
                logger.LogError("root user already exists");
                return null;
            }

            var user = new User
            {
                Name = "root",
                PasswordHash = CalcPasswordHash(password),
                IsAdmin = true,
                IsHidden = true,
            };

            dbContext.Users.Add(user);
            try
            {
                dbContext.SaveChanges();
                logger.LogInformation("root user created");
                return user;
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return null;
            }
        }

        public static User AuthAsRoot(string password)
        {
            var passwordHash = CalcPasswordHash(password);
            using var dbContext = GetDbContext();
            var rootUser = dbContext
                .Users
                .Where(u => u.Name == "root" && u.IsAdmin && u.IsHidden && u.PasswordHash == passwordHash)
                .SingleOrDefault();

            if (rootUser == default)
                logger.LogError("root user authorization failed");

            return rootUser;
        }

        #endregion
    }
}
