# Trickle Platform Testing Strategy

## Testing Pyramid

The Trickle platform follows a testing pyramid approach:

1. **Unit Tests** - Test individual components in isolation
2. **Integration Tests** - Test component interactions
3. **End-to-End Tests** - Test complete flows

## Unit Testing

### Repository Testing

Use SQLite in-memory database for repository testing:

```csharp
[Fact]
public async Task GetById_ShouldReturnCorrectEntity()
{
    // Arrange
    await using var connection = new SqliteConnection("DataSource=:memory:");
    await connection.OpenAsync();
    
    // Create schema and tables
    await CreateTestTableAsync(connection);
    
    // Insert test data
    await InsertTestDataAsync(connection);
    
    var repository = new VulnerableClusterRepository(
        new TestConnectionFactory(connection),
        _logger);
    
    // Act
    var result = await repository.GetByIdAsync("test-cluster-1");
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("test-cluster-1", result.Id);
    Assert.Equal("Test Cluster 1", result.ClusterName);
}
```
## Event Processor Testing
Use mocks for dependencies:
```csharp
[Fact]
public async Task ProcessEvent_ShouldUpdateVulnerabilityState()
{
    // Arrange
    var mockRepository = new Mock<IVulnerableClusterRepository>();
    var mockEventPublisher = new Mock<IEventPublisher>();
    var mockTenancyResolver = new Mock<ITenancyResolver>();
    
    var processor = new VulnerabilityAnalyzer(
        Mock.Of<ILogger<VulnerabilityAnalyzer>>(),
        mockRepository.Object,
        mockEventPublisher.Object,
        mockTenancyResolver.Object);
    
    var testEvent = new ContainerVulnerabilityEvent
    {
        EventId = "test-event-1",
        ClusterId = "test-cluster-1",
        CveId = "CVE-2023-12345",
        Severity = SecuritySeverity.Critical
    };
    
    // Act
    await processor.ProcessEventAsync(testEvent);
    
    // Assert
    mockRepository.Verify(r => r.UpdateAsync(
        It.Is<VulnerableClusterState>(s => 
            s.Id == "test-cluster-1" && 
            s.VulnerabilityCount > 0), 
        It.IsAny<CancellationToken>()));
        
    mockEventPublisher.Verify(p => p.PublishEventAsync(
        It.Is<SecurityEvent>(e => e.EventType == "VulnerabilityNotificationEvent"),
        It.IsAny<CancellationToken>()));
}
```

## Integration Testing
### Database Integration Testing
Test with real PostgreSQL instance (using testcontainers):
```csharp
public class PostgresIntegrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    
    public PostgresIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Repository_ShouldStoreAndRetrieve()
    {
        // Arrange
        var connectionFactory = new PostgresConnectionFactory(
            _fixture.ConnectionString,
            Mock.Of<ILogger<PostgresConnectionFactory>>());
            
        var repository = new VulnerableClusterRepository(
            connectionFactory,
            Mock.Of<ILogger<VulnerableClusterRepository>>());
            
        var entity = new VulnerableClusterState
        {
            Id = $"test-{Guid.NewGuid()}",
            ClusterName = "Test Cluster",
            VulnerabilityCount = 5
        };
        
        // Act
        await repository.CreateAsync(entity);
        var result = await repository.GetByIdAsync(entity.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.ClusterName, result.ClusterName);
        Assert.Equal(entity.VulnerabilityCount, result.VulnerabilityCount);
    }
}
```

### Event Grid Integration Testing

Test with local Event Grid emulator or real Event Grid instance:
```csharp
[Fact]
public async Task PublishEvent_ShouldDeliverToSubscriber()
{
    // Arrange
    var eventPublisher = new EventGridPublisher(
        new EventGridClientFactory(_configuration),
        Mock.Of<ILogger<EventGridPublisher>>());
        
    var testEvent = new ContainerVulnerabilityEvent
    {
        EventId = Guid.NewGuid().ToString(),
        EventType = "VulnerabilityDetected",
        Severity = SecuritySeverity.Critical
    };
    
    var receivedEvent = new TaskCompletionSource<EventGridEvent>();
    
    // Start local subscriber
    using var subscriber = new LocalEventGridSubscriber();
    subscriber.EventReceived += (sender, e) => 
    {
        if (e.Id == testEvent.EventId)
        {
            receivedEvent.SetResult(e);
        }
    };
    
    // Act
    await eventPublisher.PublishEventAsync(testEvent);
    
    // Assert - Wait for event to be received
    var result = await receivedEvent.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
    Assert.NotNull(result);
    Assert.Equal(testEvent.EventId, result.Id);
    Assert.Equal($"Trickle.Security.{testEvent.EventType}", result.EventType);
}
```

## End-to-End Testing
### Function Flow Testing
Test full function flow with actual azure functions:
```csharp
[Fact]
public async Task VulnerabilityFlow_ShouldCreateAlertAndNotification()
{
    // Arrange
    var functionApp = new TestFunctionHost();
    
    // Start collectors, analyzers, and responders
    await functionApp.StartAsync();
    
    // Mock external APIs
    var mockStackRox = new MockStackRoxServer();
    mockStackRox.AddVulnerability(new Vulnerability 
    {
        CveId = "CVE-2023-12345",
        Severity = "Critical",
        AffectedPackage = "test-package"
    });
    
    // Mock notification endpoints
    var mockTeamsEndpoint = new MockTeamsWebhook();
    
    // Act - Trigger collector function
    await functionApp.InvokeTimerFunctionAsync("CollectVulnerabilities");
    
    // Assert - Check state in database
    using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgreSQL"));
    await connection.OpenAsync();
    
    var sql = "SELECT * FROM trickle_container_state.vulnerabilities WHERE cve_id = @CveId";
    var vulnerability = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { CveId = "CVE-2023-12345" });
    
    Assert.NotNull(vulnerability);
    
    // Wait for notification
    var notification = await mockTeamsEndpoint.WaitForNotificationAsync(TimeSpan.FromSeconds(10));
    Assert.NotNull(notification);
    Assert.Contains("CVE-2023-12345", notification.Text);
}

## Test Data Management
### Reference Data
Create consistent test reference data for use in tests:
```csharp
public static class TestReferenceData
{
    public static List<ClusterReference> GetTestClusters()
    {
        return new List<ClusterReference>
        {
            new ClusterReference
            {
                Id = "test-cluster-1",
                Name = "Test Cluster 1",
                SubscriptionId = "test-sub-1",
                ResourceGroup = "test-rg-1"
            },
            new ClusterReference
            {
                Id = "test-cluster-2",
                Name = "Test Cluster 2",
                SubscriptionId = "test-sub-2",
                ResourceGroup = "test-rg-2"
            }
        };
    }
    
    public static List<CveReference> GetTestCves()
    {
        return new List<CveReference>
        {
            new CveReference
            {
                Id = "CVE-2023-12345",
                Description = "Test vulnerability",
                Severity = "Critical",
                CvssScore = 9.8f
            }
        };
    }
}
```
## Test automation

### CI/CD Pipeline Integration
Configure CI/CD pipeline to run tests automatically:
```yaml
trigger:
- main
- refactor/*

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Run Unit Tests'
  inputs:
    command: 'test'
    projects: 'tests/Trickle.UnitTests/*.csproj'
    arguments: '--configuration Release'

- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests'
  inputs:
    command: 'test'
    projects: 'tests/Trickle.IntegrationTests/*.csproj'
    arguments: '--configuration Release --filter "Category=Integration"'
```