using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Gateway.Responces;
using Ant0nRocket.Lib.Std20.Logging;

using System.ComponentModel.DataAnnotations;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    /// <summary>
    /// A gateway for document-oriented database.<br />
    /// How to use:<br />
    /// 1. Call <see cref="RegisterContextGetter(Func{IDodbContext})"/> 
    ///  and register a context getter.<br />
    /// 2. Call <see cref="RegisterDtoHandler(Dictionary{Type, Func{object, GatewayResponse}})"/>
    ///  and register a DTO handler.<br />
    /// 3. Use <see cref="PushDto{T}(Dto{T})"/>.
    /// </summary>
    public static class DodbGateway
    {
        private static readonly Logger logger = Logger.Create(nameof(DodbGateway));

        static DodbGateway()
        {
            Logger.OnLog += Logger_OnLog;
        }

        private static void Logger_OnLog(object sender, (DateTime Date, string Message, LogLevel Level, string SenderClassName, string SenderMethodName) e)
        {
            BasicLogWritter.WriteToLog(e.Date, e.Message, e.Level, e.SenderClassName, e.SenderMethodName);
        }

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

        #region Dto handlers

        /*
         And this section is about handling DTOs that pushed here from somewhere.
         Again, we dont know which function of which service handles DTO, so you have to
         register a Dictionaty of <[DtoType>, Func<dto as object, GatewayResponse>>.
         */

        public static Dictionary<Type, Func<Dto, GatewayResponse>> DtoHandleMap = new();


        ////public static void RegisterDtoHandler<T>(Func<U, GatewayResponse> handler) where T : class, new()
        ////{
        ////    var type = typeof(T);
        ////    if (dtoHandleMap.ContainsKey(type))
        ////        throw new ArgumentException($"Handler for type '{type}' already registred");

        ////    //var value = (Func<Dto, GatewayResponse>)handler;
        ////    dtoHandleMap.Add(type, handler);
        ////    logger.LogTrace($"Handler registred for type '{type}'");
        ////}

        #endregion


        public static GatewayResponse PushDto<TPayload>(U dto) where TPayload : class, new()
        {
            var type = typeof(TPayload);
            if (!DtoHandleMap.ContainsKey(type))
            {
                logger.LogError($"Handler of payload-type '{type}' are not registred");
                return new GrDtoHandlerNotFount() { Value = dto };
            }

            if (!IsDtoPropertiesValid(dto))
            {
                const string MESSAGE = "DTO with invalid IDto properties received";
                logger.LogError(MESSAGE);
                return new GrDtoIsInvalid(MESSAGE);
            }

            var validationErrors = ValidatePayload(dto);
            if (validationErrors.Count > 0)
            {
                var validationErrorsJoined = string.Join(", ", validationErrors);
                logger.LogError(validationErrorsJoined);
                return new GrDtoIsInvalid(validationErrors);
            }

            return DtoHandleMap[type](dto);
        }

        /// <summary>
        /// Returnes true if <see cref="IDto"/> properties has some values.
        /// Othervise - false.
        /// </summary>
        private static bool IsDtoPropertiesValid(Dto dto) =>
            dto.Id != Guid.Empty &&
            dto.UserId != Guid.Empty &&
            dto.DateCreated != DateTime.MinValue;

        /// <summary>
        /// Returnes a list of errors inside a payload.<br />
        /// If the list is empty then there are no errors found.
        /// </summary>
        private static List<string> ValidatePayload(U dto)
        {
            var errorsList = new List<string>();
            var validationContext = new ValidationContext(dto.Payload);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto.Payload, validationContext, validationResults, validateAllProperties: true))
                validationResults.ForEach(vr => errorsList.Add(vr.ErrorMessage));
            return errorsList;
        }
    }
}
