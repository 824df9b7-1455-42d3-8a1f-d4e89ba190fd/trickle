using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Trickle.Common.Persistence
{
    /// <summary>
    /// Configuration options for the JSON file persistence service
    /// </summary>
    public class JsonFilePersistenceOptions
    {
        /// <summary>
        /// Base directory where state files will be stored
        /// </summary>
        public string BaseDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "Trickle", "StateData");
        
        /// <summary>
        /// File extension for state files
        /// </summary>
        public string FileExtension { get; set; } = ".json";
        
        /// <summary>
        /// JSON serialization options
        /// </summary>
        public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }
    
    /// <summary>
    /// Implementation of IPersistenceService that uses JSON files for persistence
    /// </summary>
    public class JsonFilePersistenceService : IPersistenceService
    {
        private readonly JsonFilePersistenceOptions _options;
        private readonly ILogger<JsonFilePersistenceService> _logger;
        
        /// <summary>
        /// Creates a new instance of JsonFilePersistenceService
        /// </summary>
        /// <param name="options">Options for the persistence service</param>
        /// <param name="logger">Logger instance</param>
        public JsonFilePersistenceService(
            IOptions<JsonFilePersistenceOptions> options,
            ILogger<JsonFilePersistenceService> logger)
        {
            _options = options?.Value ?? new JsonFilePersistenceOptions();
            _logger = logger;
            
            // Ensure the base directory exists
            Directory.CreateDirectory(_options.BaseDirectory);
        }
        
        /// <inheritdoc />
        public async Task SaveStateAsync<T>(string id, T state, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID cannot be null or empty", nameof(id));
            if (state == null) throw new ArgumentNullException(nameof(state));
            
            var filePath = GetFilePath(id);
            
            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Serialize the state to JSON
                var json = JsonSerializer.Serialize(state, _options.SerializerOptions);
                
                // Write to file
                await File.WriteAllTextAsync(filePath, json, cancellationToken);
                
                _logger.LogDebug("State saved successfully for ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save state for ID: {Id}", id);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<T> LoadStateAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID cannot be null or empty", nameof(id));
            
            var filePath = GetFilePath(id);
            
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("No state file found for ID: {Id}", id);
                return null;
            }
            
            try
            {
                // Read the JSON file
                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                
                // Deserialize the state
                var state = JsonSerializer.Deserialize<T>(json, _options.SerializerOptions);
                
                _logger.LogDebug("State loaded successfully for ID: {Id}", id);
                return state;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load state for ID: {Id}", id);
                throw;
            }
        }
        
        /// <inheritdoc />
        public Task DeleteStateAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID cannot be null or empty", nameof(id));
            
            var filePath = GetFilePath(id);
            
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    _logger.LogDebug("State deleted successfully for ID: {Id}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete state for ID: {Id}", id);
                    throw;
                }
            }
            else
            {
                _logger.LogDebug("No state file found to delete for ID: {Id}", id);
            }
            
            return Task.CompletedTask;
        }
        
        /// <inheritdoc />
        public Task<bool> StateExistsAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID cannot be null or empty", nameof(id));
            
            var filePath = GetFilePath(id);
            return Task.FromResult(File.Exists(filePath));
        }
        
        /// <summary>
        /// Converts an ID to a file path
        /// </summary>
        /// <param name="id">The ID to convert</param>
        /// <returns>The file path for the ID</returns>
        private string GetFilePath(string id)
        {
            // Sanitize the ID to be used as a filename
            var sanitizedId = string.Join("_", id.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_options.BaseDirectory, sanitizedId + _options.FileExtension);
        }
    }
}