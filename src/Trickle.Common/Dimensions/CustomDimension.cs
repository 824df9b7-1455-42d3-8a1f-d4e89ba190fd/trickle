using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Trickle.Common.Dimensions
{
    /// <summary>
    /// Dimension implementation that loads data from a custom source
    /// </summary>
    /// <typeparam name="T">Type of dimension entity</typeparam>
    public class CustomDimension<T> : IDimension<T> where T : class
    {
        private readonly Func<CancellationToken, Task<IReadOnlyList<T>>> _dataLoader;
        private readonly Func<T, string> _keySelector;
        private readonly ILogger _logger;
        private readonly TimeSpan _cacheDuration;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKeyPrefix;
        
        /// <summary>
        /// Name of the dimension
        /// </summary>
        public string DimensionName { get; }
        
        /// <summary>
        /// When the dimension was last refreshed
        /// </summary>
        public DateTime LastRefreshed { get; private set; }
        
        /// <summary>
        /// Initialize a new custom dimension
        /// </summary>
        /// <param name="dataLoader">Function to load dimension data</param>
        /// <param name="keySelector">Function to select the key from an entity</param>
        /// <param name="logger">Logger</param>
        /// <param name="cacheDuration">Duration to cache data</param>
        /// <param name="dimensionName">Name of the dimension (defaults to type name)</param>
        public CustomDimension(
            Func<CancellationToken, Task<IReadOnlyList<T>>> dataLoader,
            Func<T, string> keySelector,
            ILogger logger,
            TimeSpan cacheDuration,
            string dimensionName = null)
        {
            _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheDuration = cacheDuration;
            DimensionName = dimensionName ?? typeof(T).Name;
            _cacheKeyPrefix = $"Dimension:{DimensionName}";
            
            // Create a memory cache if not provided
            _cache = new MemoryCache(new MemoryCacheOptions());
            
            LastRefreshed = DateTime.MinValue;
        }
        
        /// <summary>
        /// Get all values in the dimension
        /// </summary>
        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Try to get from cache first
            var cacheKey = _cacheKeyPrefix + ":All";
            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<T> cachedItems))
            {
                return cachedItems;
            }
            
            // Not in cache, load data from source
            _logger.LogDebug("Cache miss for dimension {DimensionName}, loading from source", DimensionName);
            
            try
            {
                var items = await _dataLoader(cancellationToken);
                
                // Cache the items
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration
                };
                
                _cache.Set(cacheKey, items, cacheOptions);
                
                // Update last refreshed timestamp
                LastRefreshed = DateTime.UtcNow;
                
                _logger.LogInformation(
                    "Refreshed dimension {DimensionName}, loaded {ItemCount} items", 
                    DimensionName, 
                    items.Count);
                
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading dimension {DimensionName} from source",
                    DimensionName);
                
                // Return empty list on error
                return Array.Empty<T>();
            }
        }
        
        /// <summary>
        /// Find entities matching the specified predicate
        /// </summary>
        public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            // Generate a cache key for this predicate
            var predicateKey = predicate.ToString();
            var cacheKey = $"{_cacheKeyPrefix}:Find:{predicateKey}";
            
            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<T> cachedResults))
            {
                return cachedResults;
            }
            
            // Get all items
            var allItems = await GetAllAsync(cancellationToken);
            
            // Apply predicate
            var compiledPredicate = predicate.Compile();
            var results = allItems.Where(compiledPredicate).ToList();
            
            // Cache results
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration
            };
            
            _cache.Set(cacheKey, results, cacheOptions);
            
            return results;
        }
        
        /// <summary>
        /// Find an entity by its key
        /// </summary>
        public async Task<T> FindByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return null;
                
            // Try to get from cache first
            var cacheKey = $"{_cacheKeyPrefix}:Key:{key}";
            if (_cache.TryGetValue(cacheKey, out T cachedItem))
            {
                return cachedItem;
            }
            
            // Get all items
            var allItems = await GetAllAsync(cancellationToken);
            
            // Find by key
            var item = allItems.FirstOrDefault(i => _keySelector(i) == key);
            
            if (item != null)
            {
                // Cache the item
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration
                };
                
                _cache.Set(cacheKey, item, cacheOptions);
            }
            
            return item;
        }
        
        /// <summary>
        /// Check if any entity matches the specified predicate
        /// </summary>
        public async Task<bool> ContainsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            // Get results
            var results = await FindAsync(predicate, cancellationToken);
            
            // Check if any matched
            return results.Count > 0;
        }
        
        /// <summary>
        /// Manually refresh the dimension cache
        /// </summary>
        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Manually refreshing dimension {DimensionName}", DimensionName);
            
            // Clear all cache entries for this dimension
            ClearCache();
            
            // Force reload from source
            await GetAllAsync(cancellationToken);
        }
        
        /// <summary>
        /// Clear all cached data for this dimension
        /// </summary>
        private void ClearCache()
        {
            // Since we're using a dedicated MemoryCache instance, we can just
            // create a new one to clear everything. In a shared cache scenario,
            // we would need to enumerate and remove specific entries.
            _cache.Dispose();
            _cache = new MemoryCache(new MemoryCacheOptions());
        }
    }
}
