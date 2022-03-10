using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Std20.Logging;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    public static class DodbDtoHandler
    {
        private static readonly Logger logger = Logger.Create(nameof(DodbDtoHandler));

        private static readonly Dictionary<Type, dynamic> dtoHandleMap = new();

        public static bool IsHandlerExists(Type type) =>
            dtoHandleMap.Any(h => h.Key == type);

        //public static bool IsHandlerExists() => IsHandlerExists(typeof(TPayload));

        public static void RegisterDtoHandler<TPayload>(Func<TPayload, IDodbContext, GatewayResponse> handler)
        {
            var type = typeof(TPayload);
            if (dtoHandleMap.ContainsKey(type))
            {
                logger.LogWarning($"Handler for type '{type}' was registred, replaced with new one");
                dtoHandleMap[type] = handler;
            }

            dtoHandleMap.Add(type, handler);
            logger.LogTrace($"Handler registred for type '{typeof(TPayload)}[{type}]'");
        }

        public static dynamic GetDtoHandler(Type type) =>
            IsHandlerExists(type) ? dtoHandleMap[type] : default;

        public static Func<T, IDodbContext, GatewayResponse> GetDtoHandler<T>()
        {
            var type = typeof(T);
            if (dtoHandleMap.ContainsKey(type))
                return dtoHandleMap[type];
            return null;
        }
    }
}
