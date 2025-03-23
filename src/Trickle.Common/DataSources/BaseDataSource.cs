using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Trickle.Common.DataSources
{
    /// <summary>
    /// Base interface for all data sources
    /// </summary>
    public interface IDataSource
    {
        /// <summary>
        /// Name of the data source
        /// </summary>
        string SourceName { get; }
        
        /// <summary>
        /// When the data source was last refreshed
        /// </summary>
        DateTime LastRefreshed { get; }
        
        /// <summary>
        /// Initialize the data source
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Interface for event sources that collect security events
    /// </summary>
    public interface IEventSource : IDataSource
    {
        /// <summary>
        /// Collect events from the source
        /// </summary>
        Task CollectEventsAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Interface for dimension sources that provide reference data
    /// </summary>
    public interface IDimensionSource<T> : IDataSource where T : class
    {
        /// <summary>
        /// Get current data from the source
        /// </summary>
        Task<IReadOnlyList<T>> GetCurrentDataAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Base implementation for data sources
    /// </summary>
    public abstract class BaseDataSource : IDataSource
    {
        /// <summary>
        /// Name of the data source (defaults to class name)
        /// </summary>
        public virtual string SourceName => GetType().Name;
        
        /// <summary>
        /// When the data source was last refreshed
        /// </summary>
        public DateTime LastRefreshed { get; protected set; }
        
        /// <summary>
        /// Initialize the data source
        /// </summary>
        public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            // Default implementation does nothing
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Track a refresh operation for telemetry
        /// </summary>
        protected void TrackRefresh(int itemCount, TimeSpan duration)
        {
            LastRefreshed = DateTime.UtcNow;
            
            // In a real implementation, this would also send metrics to 
            // Application Insights or another telemetry system
            
            // Example telemetry:
            // _telemetryClient.TrackMetric("DataSource.RefreshDuration", duration.TotalMilliseconds, 
            //    new Dictionary<string, string> { { "SourceName", SourceName } });
            // _telemetryClient.TrackMetric("DataSource.ItemCount", itemCount, 
            //    new Dictionary<string, string> { { "SourceName", SourceName } });
        }
    }
}
