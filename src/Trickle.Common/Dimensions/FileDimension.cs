using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Trickle.Common.Dimensions
{
    /// <summary>
    /// Dimension implementation that loads data from a JSON file
    /// </summary>
    /// <typeparam name="T">Type of dimension entity</typeparam>
    public class FileDimension<T> : CustomDimension<T> where T : class
    {
        private readonly string _filePath;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initialize a new file dimension
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <param name="keySelector">Function to select the key from an entity</param>
        /// <param name="logger">Logger</param>
        /// <param name="cacheDuration">Duration to cache data</param>
        /// <param name="dimensionName">Name of the dimension (defaults to type name)</param>
        public FileDimension(
            string filePath,
            Func<T, string> keySelector,
            ILogger logger,
            TimeSpan cacheDuration = default,
            string dimensionName = null)
            : base(ct => LoadFromFileAsync(filePath, logger, ct), 
                  keySelector, 
                  logger, 
                  cacheDuration == default ? TimeSpan.FromMinutes(15) : cacheDuration, 
                  dimensionName)
        {
            _filePath = filePath;
            _logger = logger;
        }
        
        /// <summary>
        /// Load data from a JSON file
        /// </summary>
        private static async Task<IReadOnlyList<T>> LoadFromFileAsync(
            string filePath, 
            ILogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                logger.LogDebug("Loading dimension data from file {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    logger.LogWarning("Dimension file not found: {FilePath}", filePath);
                    return Array.Empty<T>();
                }
                
                // Read file content
                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                
                // Try to deserialize as array first
                try
                {
                    var items = JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    logger.LogDebug("Loaded {ItemCount} items from file {FilePath}", items?.Count ?? 0, filePath);
                    
                    return items ?? new List<T>();
                }
                catch
                {
                    // Try deserializing as container object with Items property
                    try
                    {
                        var container = JsonSerializer.Deserialize<DimensionContainer<T>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        var items = container?.Items ?? Array.Empty<T>();
                        
                        logger.LogDebug("Loaded {ItemCount} items from container in {FilePath}", items.Length, filePath);
                        
                        return items;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to deserialize dimension data from {FilePath}", filePath);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading dimension data from file {FilePath}", filePath);
                throw;
            }
        }
        
        /// <summary>
        /// Container for dimension items in JSON files
        /// </summary>
        private class DimensionContainer<TItem>
        {
            /// <summary>
            /// Version of the container
            /// </summary>
            public string Version { get; set; }
            
            /// <summary>
            /// Last updated timestamp
            /// </summary>
            public DateTime LastUpdated { get; set; }
            
            /// <summary>
            /// Dimension items
            /// </summary>
            public TItem[] Items { get; set; } = Array.Empty<TItem>();
        }
    }
}
