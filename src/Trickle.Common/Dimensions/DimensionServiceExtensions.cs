using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Trickle.Common.Dimensions
{
    /// <summary>
    /// Extension methods for registering dimension services
    /// </summary>
    public static class DimensionServiceExtensions
    {
        /// <summary>
        /// Add dimension framework core services
        /// </summary>
        public static IServiceCollection AddDimensionFramework(this IServiceCollection services)
        {
            // Add memory cache if not already registered
            services.AddMemoryCache();
            
            return services;
        }
        
        /// <summary>
        /// Add a dimension to the service collection
        /// </summary>
        /// <typeparam name="T">Type of dimension entity</typeparam>
        /// <param name="services">Service collection</param>
        /// <param name="factory">Factory function to create the dimension</param>
        /// <param name="refreshInterval">Optional refresh interval for background updates</param>
        /// <returns>Updated service collection</returns>
        public static IServiceCollection AddDimension<T>(
            this IServiceCollection services,
            Func<DimensionRegistrationOptions, IDimension<T>> factory,
            TimeSpan? refreshInterval = null) where T : class
        {
            // Register the dimension
            services.AddSingleton<IDimension<T>>(sp =>
            {
                var options = new DimensionRegistrationOptions
                {
                    Name = typeof(T).Name,
                    ServiceProvider = sp,
                    RefreshInterval = refreshInterval ?? TimeSpan.FromMinutes(15)
                };
                
                return factory(options);
            });
            
            // Add background service for refreshing if interval specified
            if (refreshInterval.HasValue)
            {
                services.AddSingleton<IHostedService>(sp =>
                {
                    var dimension = sp.GetRequiredService<IDimension<T>>();
                    var logger = sp.GetRequiredService<ILogger<DimensionUpdateService<T>>>();
                    
                    return new DimensionUpdateService<T>(dimension, logger, refreshInterval.Value);
                });
            }
            
            return services;
        }
    }
    
    /// <summary>
    /// Background service for updating dimensions on a schedule
    /// </summary>
    public class DimensionUpdateService<T> : BackgroundService where T : class
    {
        private readonly IDimension<T> _dimension;
        private readonly ILogger<DimensionUpdateService<T>> _logger;
        private readonly TimeSpan _updateInterval;
        
        public DimensionUpdateService(
            IDimension<T> dimension,
            ILogger<DimensionUpdateService<T>> logger,
            TimeSpan updateInterval)
        {
            _dimension = dimension;
            _logger = logger;
            _updateInterval = updateInterval;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Starting dimension update service for {DimensionName} with interval {UpdateInterval}",
                _dimension.DimensionName,
                _updateInterval);
                
            // Initial update
            await RefreshDimensionAsync(stoppingToken);
                
            // Regular updates on interval
            using var timer = new PeriodicTimer(_updateInterval);
            
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshDimensionAsync(stoppingToken);
            }
        }
        
        private async Task RefreshDimensionAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Refreshing dimension {DimensionName}", _dimension.DimensionName);
                await _dimension.RefreshAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error refreshing dimension {DimensionName}",
                    _dimension.DimensionName);
            }
        }
    }
}
