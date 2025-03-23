using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trickle.Common.Persistence.Improved
{
    /// <summary>
    /// Service that handles saving and loading persistent state with enhanced reliability and security
    /// </summary>
    public interface IPersistenceService
    {
        /// <summary>
        /// Save state to persistent storage with encryption
        /// </summary>
        /// <typeparam name="T">Type of the state</typeparam>
        /// <param name="id">Unique identifier for the state</param>
        /// <param name="state">The state to save</param>
        /// <param name="securityLevel">The level of encryption to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveStateAsync<T>(string id, T state, SecurityLevel securityLevel = SecurityLevel.Standard, CancellationToken cancellationToken = default) where T : class;
        
        /// <summary>
        /// Load state from persistent storage
        /// </summary>
        /// <typeparam name="T">Type of the state</typeparam>
        /// <param name="id">Unique identifier for the state</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The loaded state, or null if not found</returns>
        Task<T> LoadStateAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;
        
        /// <summary>
        /// Delete state from persistent storage
        /// </summary>
        /// <param name="id">Unique identifier for the state</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteStateAsync(string id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Check if state exists in persistent storage
        /// </summary>
        /// <param name="id">Unique identifier for the state</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the state exists, false otherwise</returns>
        Task<bool> StateExistsAsync(string id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Bulk save multiple states in a single transaction for improved performance
        /// </summary>
        /// <typeparam name="T">Type of the states</typeparam>
        /// <param name="states">Dictionary of ID to state mappings</param>
        /// <param name="securityLevel">The level of encryption to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task BulkSaveStatesAsync<T>(IDictionary<string, T> states, SecurityLevel securityLevel = SecurityLevel.Standard, CancellationToken cancellationToken = default) where T : class;
        
        /// <summary>
        /// Bulk load multiple states for improved performance
        /// </summary>
        /// <typeparam name="T">Type of the states</typeparam>
        /// <param name="ids">Collection of IDs to load</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary mapping IDs to loaded states</returns>
        Task<IDictionary<string, T>> BulkLoadStatesAsync<T>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where T : class;
        
        /// <summary>
        /// Create a backup of the specified state
        /// </summary>
        /// <param name="id">ID of the state to backup</param>
        /// <param name="backupId">ID for the backup (defaults to timestamp-based ID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The ID of the created backup</returns>
        Task<string> CreateBackupAsync(string id, string backupId = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Restore a state from a backup
        /// </summary>
        /// <param name="id">ID of the state to restore to</param>
        /// <param name="backupId">ID of the backup to restore from</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RestoreFromBackupAsync(string id, string backupId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// List available backups for a state
        /// </summary>
        /// <param name="id">ID of the state</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of backup IDs with creation timestamps</returns>
        Task<IEnumerable<(string BackupId, DateTime CreatedAt)>> ListBackupsAsync(string id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Purge cached data to free up memory
        /// </summary>
        void PurgeCache();
        
        /// <summary>
        /// Get storage metrics for monitoring and cost analysis
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage usage metrics</returns>
        Task<StorageMetrics> GetStorageMetricsAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Security level for stored data
    /// </summary>
    public enum SecurityLevel
    {
        /// <summary>
        /// No encryption, data stored as plain text
        /// </summary>
        None,
        
        /// <summary>
        /// Standard encryption suitable for most scenarios
        /// </summary>
        Standard,
        
        /// <summary>
        /// High security encryption for sensitive data
        /// </summary>
        High
    }
    
    /// <summary>
    /// Storage metrics for monitoring and cost analysis
    /// </summary>
    public class StorageMetrics
    {
        /// <summary>
        /// Total storage space used in bytes
        /// </summary>
        public long TotalStorageUsed { get; set; }
        
        /// <summary>
        /// Total number of stored objects
        /// </summary>
        public int TotalObjectCount { get; set; }
        
        /// <summary>
        /// Number of read operations since last reset
        /// </summary>
        public int ReadOperationCount { get; set; }
        
        /// <summary>
        /// Number of write operations since last reset
        /// </summary>
        public int WriteOperationCount { get; set; }
        
        /// <summary>
        /// Cache hit ratio (0.0 to 1.0)
        /// </summary>
        public double CacheHitRatio { get; set; }
        
        /// <summary>
        /// Timestamp when metrics were last reset
        /// </summary>
        public DateTime LastResetTime { get; set; }
    }
}