using System.Threading;
using System.Threading.Tasks;

namespace Trickle.Common.Persistence
{
    /// <summary>
    /// Interface for objects that can persist their state
    /// </summary>
    public interface IPersistable
    {
        /// <summary>
        /// Unique identifier for this persistable object
        /// </summary>
        string PersistenceId { get; }
        
        /// <summary>
        /// Save the object's state
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Load the object's state
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task LoadAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Clear the object's persisted state
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ClearPersistedStateAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Generic interface for objects that can persist their state
    /// </summary>
    /// <typeparam name="T">Type of the state to persist</typeparam>
    public interface IPersistable<T> : IPersistable where T : class, new()
    {
        /// <summary>
        /// The current state of the object
        /// </summary>
        T State { get; set; }
    }
}