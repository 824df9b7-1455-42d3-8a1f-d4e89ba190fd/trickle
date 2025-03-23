using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Trickle.Common.Dimensions
{
    /// <summary>
    /// Interface for dimension repositories that provide reference data
    /// </summary>
    public interface IDimension<T> where T : class
    {
        /// <summary>
        /// Get all values in the dimension
        /// </summary>
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Find entities matching the specified predicate
        /// </summary>
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Find an entity by its key
        /// </summary>
        Task<T> FindByKeyAsync(string key, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Check if any entity matches the specified predicate
        /// </summary>
        Task<bool> ContainsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Manually refresh the dimension cache
        /// </summary>
        Task RefreshAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get the name of this dimension
        /// </summary>
        string DimensionName { get; }
        
        /// <summary>
        /// When the dimension was last refreshed
        /// </summary>
        DateTime LastRefreshed { get; }
    }
    
    /// <summary>
    /// Registration options for dimensions
    /// </summary>
    public class DimensionRegistrationOptions
    {
        /// <summary>
        /// Refresh interval for the dimension
        /// </summary>
        public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(15);
        
        /// <summary>
        /// Name of the dimension
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Service provider for dependency resolution
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }
    }
}
