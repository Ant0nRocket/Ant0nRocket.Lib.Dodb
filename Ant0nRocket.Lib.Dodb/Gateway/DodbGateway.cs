using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Entities;
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

        private static IDodbContext GetContext() =>
            contextGetter?.Invoke();

        #endregion


        public static GatewayResponse PushDto<TPayload>(DtoOf<TPayload> dto) where TPayload : class, new()
        {
            var type = typeof(TPayload);
            if (!DodbDtoHandler<TPayload>.IsHandlerExists())
            {
                logger.LogError($"Handler of payload-type '{type}' are not registred");
                return new GrDtoHandlerNotFound() { Value = dto };
            }

            var validationErrors = ValidatePayload(dto);
            if (validationErrors.Count > 0)
            {
                var validationErrorsJoined = string.Join(", ", validationErrors);
                logger.LogError(validationErrorsJoined);
                return new GrDtoIsInvalid(validationErrors);
            }

            return ApplyDto(dto);
        }

        private static GatewayResponse ApplyDto<TPayload>(DtoOf<TPayload> dto) where TPayload : class, new()
        {
            using var dbContext = GetContext();

            if (dbContext.Documents.AsNoTracking().Any(d => d.Id == dto.Id))
            {
                logger.LogWarning($"Document with Id '{dto.Id}' already exists");
                return new GrDocumentExists { DocumentId = dto.Id };
            }

            // By this step all checks are done, let's check required document (if specified) exists
            if (dto.RequiredDocumentId != default)
                if (!dbContext.Documents.AsNoTracking().Any(d => d.Id == dto.RequiredDocumentId))
                {
                    logger.LogError($"DTO '{dto.Id}' requires Document '{dto.RequiredDocumentId}' which is not found");
                    return new GrDatabaseError();
                }

            using var transaction = (dbContext as DbContext).Database.BeginTransaction();

            var document = new Document()
            {
                Id = dto.Id,
                AuthorId = dto.UserId,
                DateCreatedUtc = dto.DateCreatedUtc,
                DtoType = $"{typeof(TPayload)}",
                DtoPayload = dto.Payload.AsJson()
            };

            dbContext.Documents.Add(document);

            var dtoSaveResult = DodbDtoHandler<TPayload>.GetDtoHandler()?.Invoke(dto, dbContext);

            if (dtoSaveResult is not GrDtoSaveSuccess)
            {
                logger.LogError($"Unable to save DTO '{dto.Id}'");
                return new GrDtoSaveFailed();
            }

            try
            {
                dbContext.SaveChanges();
                transaction.Commit();
                logger.LogInformation($"DTO '{dto.Id}' saved");
                return new GrDtoSaveSuccess();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                transaction.Rollback();
                return new GrDtoSaveFailed();
            }

        }

        /// <summary>
        /// Returnes a list of errors inside a payload.<br />
        /// If the list is empty then there are no errors found.
        /// </summary>
        private static List<string> ValidatePayload<TPayload>(DtoOf<TPayload> dto) where TPayload : class, new()
        {
            var errorsList = new List<string>();

            // Check basic properties
            if (dto.Id == Guid.Empty) errorsList.Add($"{nameof(dto.Id)} is not set");
            if (dto.UserId == Guid.Empty) errorsList.Add($"{nameof(dto.UserId)} is not set");
            if (dto.DateCreatedUtc == DateTime.MinValue) errorsList.Add($"{nameof(dto.DateCreatedUtc)} is not set");

            // Check payload using annotations
            var validationContext = new ValidationContext(dto.Payload);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto.Payload, validationContext, validationResults, validateAllProperties: true))
                validationResults.ForEach(vr => errorsList.Add(vr.ErrorMessage));

            return errorsList;
        }
    }
}
