using Ant0nRocket.Lib.Dodb.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace Ant0nRocket.Lib.Dodb.Services
{
    public static class Services
    {
        private static IServiceCollection serviceCollection { get; set; }

        private static IServiceProvider serviceProvider { get; set; }

        static Services()
        {
            serviceCollection = new ServiceCollection();
            serviceProvider = default;

            RegisterServicesInCollection();
        }

        private static void RegisterServicesInCollection()
        {
            serviceCollection.AddSingleton<IDodbDocumentsService, DodbDocumentsService>();
        }

        /// <summary>
        /// For NON-HOST configurations.
        /// </summary>
        public static TService GetService<TService>() 
        {
            serviceProvider ??= serviceCollection.BuildServiceProvider();
            return serviceProvider.GetRequiredService<TService>();
        }

        /// <summary>
        /// For HOSTED configurations.
        /// </summary>
        public static void UseDodb(this IServiceCollection services)
        {
            serviceCollection = services;
            serviceProvider = default;

            RegisterServicesInCollection();            
        }
    }
}
