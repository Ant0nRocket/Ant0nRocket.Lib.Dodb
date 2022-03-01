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

        #region DbContext getter

        private static Func<IDodbContext> contextGetter;

        public static void RegisterContextGetter(Func<IDodbContext> value) =>
            contextGetter = value;

        private static IDodbContext GetContext() =>
            contextGetter?.Invoke();

        #endregion

        #region Dto handlers

        private static Dictionary<Type, Func<object, GatewayResponse>> dtoHandleMap;

        public static void RegisterDtoHandler(Dictionary<Type, Func<object, GatewayResponse>> value) =>
            dtoHandleMap = value;

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
