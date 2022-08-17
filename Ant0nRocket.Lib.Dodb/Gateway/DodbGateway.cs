using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Attributes;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway.Helpers;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
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
        private static readonly Logger logger = Logger.Create(nameof(DodbGateway));

        #region DTO handling

        private static bool? _isAnyDocumentExist = null;

        public static bool IsAnyDocumentExist => _isAnyDocumentExist == true ? true : false;

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

            if (_isAnyDocumentExist == null)
            {
                _isAnyDocumentExist = CheckDocumentExist(dbContext);
                logger.LogDebug($"Any documents found? - {_isAnyDocumentExist}");
            }

            #endregion

            #region 2. Check document doesn't exists, check required document exists

            // Check user exists (if it's not first document!)
            if (!CheckUserExists(dbContext, dto.UserId)) // such user not found
            {
                if (_isAnyDocumentExist == true) // ...but some documents already exists
                {
                    logger.LogError($"DTO from unknown user '{dto.UserId}' received");
                    return new GrDtoFromUnknownUser { UserId = dto.UserId };
                }
            }

            // Check document with dto.Id doesn't exist
            if (CheckDocumentExist(dbContext, dto.Id))
            {
                logger.LogError($"Document with Id='{dto.Id}' already exists");
                return new GrDocumentExists { DocumentId = dto.Id };
            }

            if (dto.RequiredDocumentId == default)
            {
                if (dto.DateCreatedUtc == DateTime.MinValue) // that means in app generated
                {
                    dto.DateCreatedUtc = DateTime.UtcNow;
                    dto.RequiredDocumentId = GetLatestDocumentId(dbContext);
                }
                else
                {
                    var message = $"Invalid DTO '{dto.Id}' with no date and no requied document ID received";
                    logger.LogError(message);
                    return new GrDtoIsInvalid(message);
                }
            }
            else // if (dto.RequiredDocumentId == default) || some required document specified in DTO
            {
                if (dto.DateCreatedUtc == DateTime.MinValue)
                {
                    var message = $"Invalid DTO '{dto.Id}' with no date but with required document ID received";
                    logger.LogError(message);
                    return new GrDtoIsInvalid(message);
                }

                if (!CheckDocumentExist(dbContext, dto.RequiredDocumentId))
                {
                    logger.LogWarning($"DTO '{dto.Id}' requires Document '{dto.RequiredDocumentId}' which is not found");
                    return new GrRequiredDocumentNotFound
                    {
                        RequesterId = dto.Id,
                        RequiredDocumentId = dto.RequiredDocumentId,
                    };
                }
            }

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

            var document = new Document
            {
                Id = dto.Id,
                UserId = dto.UserId,
                RequiredDocumentId = dto.RequiredDocumentId,
                DateCreatedUtc = dto.DateCreatedUtc,
                Description = dto.Description,
                PayloadType = $"{dto.Payload.GetType()}",
                Payload = dto.Payload.AsJson()
            };

            dbContext.Documents.Add(document);

            logger.LogInformation($"[uncommited] DTO '{dto.Id}' applied");

            #endregion

            return dtoHandleResponse;
        }

        /// <summary>
        /// Checks specified by <paramref name="userId"/> user exists.
        /// </summary>
        private static bool CheckUserExists(IDodbContext dbContext, Guid userId = default)
        {
            return dbContext.Users.AsNoTracking().Any(u => u.Id == userId);
        }

        /// <summary>
        /// Checks any document (default <paramref name="documentId"/>) or
        /// specified by <paramref name="documentId"/> exists.
        /// </summary>
        private static bool CheckDocumentExist(IDodbContext dbContext, Guid documentId = default)
        {
            var query = dbContext.Documents.AsNoTracking();

            if (documentId != default)
                query = query.Where(d => d.Id == documentId);

            return query.Any();
        }

        /// <summary>
        /// Gets the latest document Id or empty Guid if no documents found.
        /// </summary>
        private static Guid GetLatestDocumentId(IDodbContext dbContext)
        {
            return dbContext
                    .Documents
                    .AsNoTracking()
                    .OrderByDescending(d => d.DateCreatedUtc)
                    .FirstOrDefault()?.Id ?? Guid.Empty;
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
            if (validator.HasFoundErrors)
            {
                logger.LogError($"Invalid DTO '{dto.Id}': {string.Join(", ", validator.ErrorsList)}");
                return new GrDtoIsInvalid(validator.ErrorsList);
            }

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

        #endregion

        #region Users managing



        #endregion
    }
}
