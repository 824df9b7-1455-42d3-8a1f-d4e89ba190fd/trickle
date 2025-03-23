using System.Threading;
using System.Threading.Tasks;

namespace Trickle.Common.Persistence
{
    /// <summary>
    /// Service that handles saving and loading persistent state
    /// </summary>
    public interface IPersistenceService
    {
        /// <summary>
        /// Save state to persistent storage
        /// </summary>
        /// <typeparam name="T">Type of the state</typeparam>
        /// <param name="id">Unique identifier for the state</param>
        /// <param name="state">The state to save</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveStateAsync<T>(string id, T state, CancellationToken cancellationToken = default) where T : class;
        
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
    }
}