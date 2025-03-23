using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Trickle.Common.Persistence.Examples
{
    /// <summary>
    /// Example state class that will be persisted
    /// </summary>
    public class ExampleState
    {
        /// <summary>
        /// Last time the state was updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Count of operations performed
        /// </summary>
        public int OperationCount { get; set; }
        
        /// <summary>
        /// Collection of items being tracked
        /// </summary>
        public List<string> Items { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Example class demonstrating the use of PersistableBase
    /// </summary>
    public class PersistableExample : PersistableBase<ExampleState>
    {
        private readonly string _id;
        
        /// <summary>
        /// Creates a new instance of PersistableExample
        /// </summary>
        /// <param name="id">Unique identifier for this instance</param>
        /// <param name="persistenceService">Service for persisting state</param>
        /// <param name="logger">Logger instance</param>
        public PersistableExample(
            string id,
            IPersistenceService persistenceService,
            ILogger<PersistableExample> logger)
            : base(persistenceService, logger)
        {
            _id = id ?? throw new ArgumentNullException(nameof(id));
        }
        
        /// <inheritdoc />
        public override string PersistenceId => $"example_{_id}";
        
        /// <summary>
        /// Adds an item to the state and persists the changes
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task AddItemAsync(string item, CancellationToken cancellationToken = default)
        {
            // Modify the state
            State.Items.Add(item);
            State.OperationCount++;
            State.LastUpdated = DateTime.UtcNow;
            
            // Persist the changes
            await SaveAsync(cancellationToken);
        }
        
        /// <summary>
        /// Removes all items from the state and persists the changes
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ClearItemsAsync(CancellationToken cancellationToken = default)
        {
            // Modify the state
            State.Items.Clear();
            State.OperationCount++;
            State.LastUpdated = DateTime.UtcNow;
            
            // Persist the changes
            await SaveAsync(cancellationToken);
        }
        
        /// <summary>
        /// Retrieves the current list of items
        /// </summary>
        /// <returns>List of items</returns>
        public IReadOnlyList<string> GetItems()
        {
            return State.Items.AsReadOnly();
        }
    }
}