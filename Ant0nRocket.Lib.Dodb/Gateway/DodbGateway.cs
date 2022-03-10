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

        private static DtoHandler dtoHandler;

        public static void RegisterDtoHandler(DtoHandler handler) => dtoHandler = handler;

        #endregion


        public static GatewayResponse PushDto<TPayload>(DtoOf<TPayload> dto) where TPayload : class, new()
        {
            // 1. Validate
            var validator = new DtoValidator<TPayload>(dto).Validate().AndLogErrorsTo(logger);
            if (validator.HasFoundErrors) return new GrDtoIsInvalid(validator.ErrorsList);

            // 2. Get dbContext
            using var dbContext = GetContext();

            // 3. Create document
            var dtoHandlingResult = new Dto2DocumentConverter()
                .WithDbContext(dbContext)
                .CreateDocumentFrom(dto)
                .AndHandleDtoWith(dtoHandler);

            return dtoHandlingResult;
        }
    }
}
