using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Trickle.Common.Persistence
{
    /// <summary>
    /// Base class for objects that can persist their state
    /// </summary>
    /// <typeparam name="T">Type of the state to persist</typeparam>
    public abstract class PersistableBase<T> : IPersistable<T> where T : class, new()
    {
        private readonly IPersistenceService _persistenceService;
        private readonly ILogger _logger;
        private T _state;
        
        /// <summary>
        /// Creates a new instance of PersistableBase
        /// </summary>
        /// <param name="persistenceService">Service for persisting state</param>
        /// <param name="logger">Logger instance</param>
        protected PersistableBase(IPersistenceService persistenceService, ILogger logger)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = new T();
        }
        
        /// <inheritdoc />
        public abstract string PersistenceId { get; }
        
        /// <inheritdoc />
        public T State 
        { 
            get => _state; 
            set => _state = value ?? new T(); 
        }
        
        /// <inheritdoc />
        public virtual async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _persistenceService.SaveStateAsync(PersistenceId, State, cancellationToken);
                _logger.LogDebug("State saved for {TypeName} with ID {Id}", typeof(T).Name, PersistenceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save state for {TypeName} with ID {Id}", typeof(T).Name, PersistenceId);
                throw;
            }
        }
        
        /// <inheritdoc />
        public virtual async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var loadedState = await _persistenceService.LoadStateAsync<T>(PersistenceId, cancellationToken);
                
                if (loadedState != null)
                {
                    State = loadedState;
                    _logger.LogDebug("State loaded for {TypeName} with ID {Id}", typeof(T).Name, PersistenceId);
                }
                else
                {
                    // Initialize with a new state if none exists
                    State = new T();
                    _logger.LogDebug("No state found for {TypeName} with ID {Id}, initialized new state", typeof(T).Name, PersistenceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load state for {TypeName} with ID {Id}", typeof(T).Name, PersistenceId);
                throw;
            }
        }
        
        /// <inheritdoc />
        public virtual async Task ClearPersistedStateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _persistenceService.DeleteStateAsync(PersistenceId, cancellationToken);
                _logger.LogDebug("Persisted state cleared for {TypeName} with ID {Id}", typeof(T).Name, PersistenceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear persisted state for {TypeName} with ID {Id}", typeof(T).Name, PersistenceId);
                throw;
            }
        }
    }
}