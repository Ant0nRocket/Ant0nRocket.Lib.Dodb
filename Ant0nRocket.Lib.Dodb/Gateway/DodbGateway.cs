using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Dodb.Gateway.Responces;
using Ant0nRocket.Lib.Std20.Logging;

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

        private static Dictionary<Type, Func<IDto, GatewayResponse>> dtoHandleMap = new();


        public static void RegisterDtoHandler<T>(Func<T, GatewayResponse> handler) where T : IDto
        {
            var type = typeof(T);
            if (dtoHandleMap.ContainsKey(type))
                throw new ArgumentException($"Handler for type '{type}' already registred");

            dtoHandleMap.Add(type, handler as Func<IDto, GatewayResponse>);
            logger.LogTrace($"Handler registred for type '{type}'");
        }

        #endregion


        public static GatewayResponse PushDto<T>(Dto<T> dto) where T : class, new()
        {
            var type = typeof(T);
            if (!dtoHandleMap.ContainsKey(type))
            {
                logger.LogError($"DTO of unregistred type '{type}' received");
                return new GrUnknownDtoPayloadType();
            }

            return dtoHandleMap[type](dto);
        }
    }
}
