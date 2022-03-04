using Ant0nRocket.Lib.Dodb.Abstractions;
using Ant0nRocket.Lib.Dodb.Dtos;
using Ant0nRocket.Lib.Std20.Logging;

namespace Ant0nRocket.Lib.Dodb.Gateway
{
    public static class DodbDtoHandler<TPayload> where TPayload : class, new()
    {
        private static readonly Logger logger = Logger.Create(nameof(DodbDtoHandler<TPayload>));


        private static readonly Dictionary<
            Type,
            ApplyDtoDelegate<TPayload>
            > dtoHandleMap = new();

        public static bool IsHandlerExists() => IsHandlerExists(typeof(DtoOf<TPayload>));

        public static bool IsHandlerExists(Type type) =>
            dtoHandleMap.Any(h => h.Key == type);

        public static void RegisterDtoHandler(ApplyDtoDelegate<TPayload> handler)
        {
            var type = typeof(DtoOf<TPayload>);
            if (dtoHandleMap.ContainsKey(type))
                throw new ArgumentException($"Handler for type '{type}' already registred");

            //var value = (Func<Dto, GatewayResponse>)handler;
            dtoHandleMap.Add(type, handler);
            logger.LogTrace($"Handler registred for type '{type}'");
        }

        public static ApplyDtoDelegate<TPayload> GetDtoHandler(Type type) =>
            IsHandlerExists(type) ? dtoHandleMap[type] : default;

        public static ApplyDtoDelegate<TPayload> GetDtoHandler() =>
            GetDtoHandler(typeof(DtoOf<TPayload>));
    }
}
