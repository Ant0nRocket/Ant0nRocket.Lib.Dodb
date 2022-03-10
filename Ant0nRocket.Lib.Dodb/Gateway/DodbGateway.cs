using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
using Ant0nRocket.Lib.Dodb.Gateway.Helpers;
using Ant0nRocket.Lib.Dodb.Gateway.Responces;
using Ant0nRocket.Lib.Std20.Extensions;
using Ant0nRocket.Lib.Std20.Logging;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;

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
            Logger.OnLog += Logger_OnLog;
        }

        private static void Logger_OnLog(object sender, (DateTime Date, string Message, LogLevel Level, string SenderClassName, string SenderMethodName) e)
        {
            BasicLogWritter.WriteToLog(e.Date, e.Message, e.Level, e.SenderClassName, e.SenderMethodName);
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


        public static GatewayResponse PushDto<TPayload>(DtoOf<TPayload> dto) where TPayload : class, new()
        {
            #region 1. Checking handler and validation

            if (dtoPayloadHandler == default) throw new NullReferenceException(nameof(dtoPayloadHandler));

            var validator = new DtoValidator<TPayload>(dto).Validate().AndLogErrorsTo(logger);
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
                AuthorId = dto.AuthorId,
                RequiredDocumentId = dto.RequiredDocumentId,
                DateCreatedUtc = dto.DateCreatedUtc,
                PayloadType = $"{dto.Payload.GetType()}",
                Payload = dto.Payload.AsJson()
            };

            dbContext.Documents.Add(document);

            #endregion

            #region 5. Handling DTO

            var dtoHandleResponse = dtoPayloadHandler(dto.Payload, dbContext);
            if (dtoHandleResponse is GrDtoPayloadHandleSuccess)
            {
                try
                {
                    transaction.Commit();
                    logger.LogInformation($"Document '{dto.Id}' saved");
                    return new GrPushDtoSuccess();
                }
                catch (Exception ex)
                {
                    logger.LogException(ex, $"Unable to proceed DTO '{dto.Id}'");
                }
            }
            else if (dtoHandleResponse is GrDtoPayloadHandlerNotFound)
            {
                logger.LogError($"Can't handle payload of DTO '{dto.Id}': no handler found");
                return dtoHandleResponse;
            }


            // transaction will be auto-rolled back when disposed, but I prefer to do it manually
            transaction.Rollback();
            return new GrPushDtoFailed();

            #endregion
        }
    }
}
