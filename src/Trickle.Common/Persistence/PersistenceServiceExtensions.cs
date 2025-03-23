using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Trickle.Common.Persistence
{
    /// <summary>
    /// Extension methods for setting up persistence services in an IServiceCollection
    /// </summary>
    public static class PersistenceServiceExtensions
    {
        /// <summary>
        /// Adds the persistence framework to the specified IServiceCollection
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to</param>
        /// <returns>The IServiceCollection so that additional calls can be chained</returns>
        public static IServiceCollection AddPersistenceFramework(this IServiceCollection services)
        {
            // Register default JSON file persistence service
            services.TryAddSingleton<IPersistenceService, JsonFilePersistenceService>();
            services.TryAddSingleton<JsonFilePersistenceOptions>();
            
            return services;
        }
        
        /// <summary>
        /// Adds the persistence framework to the specified IServiceCollection with custom options
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to</param>
        /// <param name="configureOptions">A delegate to configure the JsonFilePersistenceOptions</param>
        /// <returns>The IServiceCollection so that additional calls can be chained</returns>
        public static IServiceCollection AddPersistenceFramework(
            this IServiceCollection services,
            Action<JsonFilePersistenceOptions> configureOptions)
        {
            services.AddPersistenceFramework();
            services.Configure(configureOptions);
            
            return services;
        }
        
        /// <summary>
        /// Adds a custom persistence service implementation
        /// </summary>
        /// <typeparam name="TService">The type of the persistence service implementation</typeparam>
        /// <param name="services">The IServiceCollection to add services to</param>
        /// <returns>The IServiceCollection so that additional calls can be chained</returns>
        public static IServiceCollection AddCustomPersistenceService<TService>(this IServiceCollection services)
            where TService : class, IPersistenceService
        {
            services.AddPersistenceFramework();
            services.RemoveAll<IPersistenceService>();
            services.AddSingleton<IPersistenceService, TService>();
            
            return services;
        }
    }
}