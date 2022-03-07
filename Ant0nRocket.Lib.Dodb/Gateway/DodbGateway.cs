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

        public static IDodbContext GetContext() =>
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
                return new GrDocumentExists { DocumentId = dto.Id }.WithLogging();

            using var transaction = (dbContext as DbContext).Database.BeginTransaction();

            var dtoSaveResult = TryApplyDtoAndSaveDocument(dto, dbContext);

            if (dtoSaveResult is not GrDtoSaveSuccess)
            {
                logger.LogError($"Unable to save DTO '{dto.Id}': {dtoSaveResult.GetType().Name} returned");
                return dtoSaveResult;
            }

            try
            {
                dbContext.SaveChanges();
                transaction.Commit();
                return new GrDtoSaveSuccess { DocumentId = dto.Id }.WithLogging();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                transaction.Rollback();
                return new GrDtoSaveFailed { DocumentId = dto.Id }.WithLogging(logLevel: LogLevel.Error);
            }

        }

        private static GatewayResponse TryApplyDtoAndSaveDocument<TPayload>(DtoOf<TPayload> dto, IDodbContext dbContext) where TPayload : class, new()
        {
            if (dto.RequiredDocumentId != default)
                if (!dbContext.Documents.AsNoTracking().Any(d => d.Id == dto.RequiredDocumentId))
                {
                    return new GrRequiredDocumentNotFound
                    {
                        RequesterId = dto.Id,
                        RequiredDocumentId = dto.RequiredDocumentId
                    }
                    .WithLogging(logLevel: LogLevel.Error);
                }

            if (dto.DateCreatedUtc == DateTime.MinValue)
            {
                dto.RequiredDocumentId = dbContext
                    .Documents.AsNoTracking()
                    .OrderByDescending(d => d.DateCreatedUtc)
                    .FirstOrDefault()?.Id ?? Guid.Empty;
                dto.DateCreatedUtc = DateTime.UtcNow;
            }

            var document = new Document()
            {
                Id = dto.Id,
                AuthorId = dto.AuthorId,
                RequiredDocumentId = dto.RequiredDocumentId,
                DateCreatedUtc = dto.DateCreatedUtc,
                DtoType = $"{typeof(TPayload)}",
                DtoPayload = dto.Payload.AsJson()
            };

            dbContext.Documents.Add(document);

            return DodbDtoHandler<TPayload>.GetDtoHandler()?.Invoke(dto, dbContext);
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
            if (dto.AuthorId == Guid.Empty) errorsList.Add($"{nameof(dto.AuthorId)} is not set");

            // Check payload using annotations
            var validationContext = new ValidationContext(dto.Payload);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto.Payload, validationContext, validationResults, validateAllProperties: true))
                validationResults.ForEach(vr => errorsList.Add(vr.ErrorMessage));

            return errorsList;
        }
    }
}
