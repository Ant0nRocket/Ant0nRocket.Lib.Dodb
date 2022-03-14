using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway.Helpers;
using Ant0nRocket.Lib.Dodb.Gateway.Responses;
using Ant0nRocket.Lib.Std20.Extensions;
using Ant0nRocket.Lib.Std20.Logging;

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

        static DodbGateway()
        {
#if DEBUG
            Logger.LogToBasicLogWritter = true;
#endif
        }

        #endregion

        #region DbContext getter

        /*
         Why we need a DbContext getter?
         This class doesn't know about the final class you will work with but it has to be IDodbContext.
         As you DbContext will be somewhere in external library we need to know how to get it.
         */

        private static Func<IDodbContext> contextGetter;

        public static void RegisterContextGetter(Func<IDodbContext> value) =>
            contextGetter = value;

        public static IDodbContext GetContext() =>
            contextGetter?.Invoke();

        #endregion

        #region DTO handler

        private static DtoPayloadHandler dtoPayloadHandler;

        public static void RegisterDtoHandler(DtoPayloadHandler handler) => dtoPayloadHandler = handler;

        #endregion

        /// <summary>
        /// 1. Throws <see cref="NullReferenceException"/> if you forgot to register ContextGetter (see <see cref="RegisterContextGetter(Func{IDodbContext})"/>).<br /> 
        /// 2. Throws <see cref="NullReferenceException"/> if you forgot to register DtoHandler (see <see cref="RegisterDtoHandler(DtoPayloadHandler)"/>).<br />
        /// 3. Returnes <see cref="GrDtoIsInvalid"/> if there are some validation errors in DTO or its payload.<br />
        /// 4. Returnes <see cref="GrDocumentExists"/> if any document with <paramref name="dto"/>.Id already exists.<br />
        /// 5. Returnes <see cref="GrRequiredDocumentNotFound"/> if there is some document required to exist but not found.<br />
        /// 6. Returnes <see cref="GrDtoPayloadHandlerNotFound"/> if there is no handler found for payload.<br />
        /// 7. Returnes <see cref="GrPushDtoFailed"/> if there some errors durring commit.<br />
        /// <br />
        /// Othervise returnes some <see cref="GatewayResponse"/>
        /// </summary>
        public static GatewayResponse PushDto<TPayload>(DtoOf<TPayload> dto) where TPayload : class, new()
        {
            #region 1. Checking handler and validation

            if (contextGetter == default) throw new NullReferenceException(nameof(contextGetter));
            if (dtoPayloadHandler == default) throw new NullReferenceException(nameof(dtoPayloadHandler));

            var validator = new DtoValidator<TPayload>(dto).Validate();
            if (validator.HasFoundErrors) return new GrDtoIsInvalid(validator.ErrorsList);

            #endregion

            #region 2. Getting dbContext and begining a new transaction

            using var dbContext = GetContext();
            using var transaction = (dbContext as DbContext).Database.BeginTransaction();

            #endregion

            #region 3. Check document doesn't exists, check required document exists

            if (dbContext.Documents.AsNoTracking().Any(d => d.Id == dto.Id))
                return new GrDocumentExists { DocumentId = dto.Id };

            if (dto.RequiredDocumentId != default) // some document ID required
                if (!dbContext.Documents.AsNoTracking().Any(d => d.Id == dto.RequiredDocumentId))
                    return new GrRequiredDocumentNotFound
                    { // if required document is not found - end. Maybe next time it will be there.
                        RequesterId = dto.Id,
                        RequiredDocumentId = dto.RequiredDocumentId
                    };

            #endregion

            #region 4. Determining DTO source and creating a Document

            if (dto.DateCreatedUtc == DateTime.MinValue) // means that DTO created in app
            {                                            // because deserialized has some value
                dto.RequiredDocumentId = dbContext
                    .Documents.AsNoTracking()
                    .OrderByDescending(d => d.DateCreatedUtc)
                    .FirstOrDefault()?.Id ?? Guid.Empty; // As we have a new DTO let's get latest document ID
                dto.DateCreatedUtc = DateTime.UtcNow;
            }

            var document = new Document
            {
                Id = dto.Id,
                AuthorId = dto.AuthToken,
                RequiredDocumentId = dto.RequiredDocumentId,
                DateCreatedUtc = dto.DateCreatedUtc,
                PayloadType = $"{dto.Payload.GetType()}",
                Payload = dto.Payload.AsJson()
            };

            dbContext.Documents.Add(document);

            #endregion

            #region 5. Handling DTO

            var dtoHandleResponse = dtoPayloadHandler(dto.Payload, dbContext);

            if (dtoHandleResponse is GrDtoPayloadHandlerNotFound)
            {
                logger.LogError($"Can't handle payload of DTO '{dto.Id}': no handler found for '{dto.Payload.GetType()}");
            }
            else
            {
                try
                {
                    dbContext.SaveChanges();
                    transaction.Commit();
                    logger.LogInformation($"Document '{dto.Id}' saved");
                }
                catch (Exception ex)
                {
                    dtoHandleResponse = new GrPushDtoFailed();
                    logger.LogException(ex, $"Unable to proceed DTO '{dto.Id}'");
                }
            }

            return dtoHandleResponse;

            #endregion
        }
    }
}
