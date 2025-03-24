# Trickle Platform Implementation Patterns

## Repository Pattern

### Base Repository

All repositories should inherit from a base repository that provides common CRUD operations:

```csharp
public abstract class DapperRepository<T> : IRepository<T> where T : class
{
    protected readonly string _tableName;
    protected readonly string _schemaName;
    protected readonly IDbConnectionFactory _connectionFactory;
    protected readonly ILogger _logger;
    
    // Common implementation...
}
```
### Domain Repositories
Domain-specific repositories extend the base with specialized queries:
public class VulnerableClusterRepository : DapperRepository<VulnerableClusterState>, IVulnerableClusterRepository
{
    // Domain-specific methods...
    
    public async Task<IReadOnlyList<VulnerableClusterState>> GetCriticalClustersAsync(
        int minVulnerabilityCount = 5,
        CancellationToken cancellationToken = default)
    {
        // Implementation...
    }
}

## Event Processing
### Event Publication
Use the Event Grid publisher for all event publication:
```csharp
public async Task PublishEventAsync<T>(T securityEvent) where T : SecurityEvent
{
    await _eventPublisher.PublishEventAsync(securityEvent);
}
```
### Event Processing
Use the processor base class for consistent event handling:
```csharp
public abstract class SecurityEventProcessor<TEvent> where TEvent : SecurityEvent
{
    // Common implementation...
    
    protected abstract Task<ProcessingResult> ProcessEventInternalAsync(
        TEvent securityEvent,
        CancellationToken cancellationToken);
}
```
## Reference Data
### Reference Repository
Use the reference repository pattern for all reference data:
```csharp
public interface IReferenceRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
```
### Reference Updater
Use updater pattern for refreshing reference data:
```csharp
public abstract class ReferenceUpdater<T> where T : class
{
    protected readonly IReferenceRepository<T> _repository;
    protected readonly ILogger _logger;
    
    protected abstract Task<IReadOnlyList<T>> FetchReferenceDataAsync(CancellationToken cancellationToken);
    
    public async Task UpdateReferenceDataAsync(CancellationToken cancellationToken = default)
    {
        // Implementation...
    }
}
```
## Function Patterns
### Timer Triggered Collectors
```csharp
public class VulnerabilityCollectorFunction
{
    [Function("CollectVulnerabilities")]
    public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo timer)
    {
        // Implementation...
    }
}
```
### Event Grid Triggered Analyzers
Use Event Grid triggers for event processing:
```csharp
public class VulnerabilityAnalyzerFunction
{
    [Function("AnalyzeVulnerabilities")]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        // Implementation...
    }
}
```
## Resilience Patterns
### Retry Policies
Use Polly for retry policies:
```csharp
private readonly AsyncPolicy _retryPolicy = Policy
    .Handle<SqlException>(ex => IsTransient(ex))
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        3, 
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        (exception, timeSpan, context) => 
        {
            // Logging...
        });
```
### Circuit Breakers
Use circuit breakers for external services:
```csharp
private readonly AsyncPolicy _circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromMinutes(1),
        onBreak: (ex, breakDelay) => 
        {
            // Logging...
        },
        onReset: () => 
        {
            // Logging...
        });
```
## Multi-Tenant Patterns
### Event Grid 
Include tenant ID in event data and subject:
```csharp
var eventGridEvent = new EventGridEvent(
    subject: $"/security/container/{securityEvent.OwnerId}/{securityEvent.ResourceId}",
    eventType: $"Trickle.Security.{securityEvent.EventType}",
    dataVersion: "1.0",
    data: securityEvent);
```
### Database access
Set tenant context in database connection:
```csharp
await using var cmd = new NpgsqlCommand(
    "SET app.tenant_id = @tenantId", 
    connection);
    
cmd.Parameters.AddWithValue("tenantId", TenantContext.Current.TenantId);
await cmd.ExecuteNonQueryAsync(cancellationToken);
```




