# Trickle Platform Naming Conventions

## Namespaces
Trickle.{Component}.{SubComponent}

Examples:
- `Trickle.Common.EventGrid`
- `Trickle.ContainerSecurity.Core.Models`
- `Trickle.Collectors.Container.Functions`

## Projects

Projects follow a similar naming pattern to namespaces:

Trickle.{Component}[.{SubComponent}]

Examples:
- `Trickle.Common`
- `Trickle.ContainerSecurity.Core`
- `Trickle.Collectors.Container`

## Classes

- Use PascalCase for class names
- Use clear, descriptive names without abbreviations
- Use appropriate suffixes that indicate the class's role:

| Suffix | Usage |
|--------|-------|
| `Repository` | For data access components |
| `Service` | For business logic components |
| `Client` | For external API clients |
| `Function` | For Azure Function entry points |
| `Event` | For event classes |
| `Analyzer` | For security analyzer components |
| `Collector` | For data collection components |
| `Responder` | For event response components |

Examples:
- `VulnerableClusterRepository`
- `StackRoxClient`
- `ContainerVulnerabilityEvent`
- `VulnerabilityCollectorFunction`

## Interfaces

- Prefix with `I`
- Use PascalCase
- Name should clearly indicate the contract

Examples:
- `IRepository<T>`
- `IEventPublisher`
- `IStackRoxClient`

## Methods

- Use PascalCase
- Use verb-noun naming
- Async methods should have `Async` suffix

Examples:
- `GetByIdAsync`
- `PublishEventAsync`
- `ProcessVulnerabilityAsync`

## Properties and Variables

- Properties use PascalCase
- Local variables use camelCase
- Private fields use camelCase with underscore prefix

Examples:
- `public string EventId { get; set; }`
- `private readonly ILogger _logger;`
- `var vulnerabilityCount = 0;`

## Database

- Schemas use snake_case: `trickle_container_state`
- Tables use snake_case: `vulnerable_clusters`
- Columns use snake_case: `vulnerability_count`
- Indexes use pattern: `ix_{table}_{columns}`

## Constants

- Use PascalCase for public constants
- Use UPPER_SNAKE_CASE for private constants

Examples:
- `public const string DefaultSchemaName = "trickle_container_state";`
- `private const string SQL_SELECT_BY_ID = "SELECT * FROM {0} WHERE id = @Id";`

## Event Grid

- Event types: `Trickle.{Domain}.{EventType}`
- Subjects: `/security/{domain}/{resource-type}/{resource-id}`

Examples:
- Event type: `Trickle.ContainerSecurity.VulnerabilityDetected`
- Subject: `/security/container/cluster/cluster-123`