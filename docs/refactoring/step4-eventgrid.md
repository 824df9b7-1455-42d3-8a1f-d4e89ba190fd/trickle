# Step 4: Event Grid Integration

## Branch: `refactor/step4-event-grid`

## Goals
- Replace Service Bus with Event Grid
- Implement clean publisher/subscriber pattern
- Support multi-tenancy and proper routing
- Integrate with Azure Functions

## Event Grid Design

### Topics and Subscriptions
- Domain-specific topics for separation
- Subscription-based filtering for routing
- Event type filtering for handlers

### Event Schema
- Standard properties for all events
- Domain-specific properties in data payload
- Consistent subject naming convention

## Core Components

1. `IEventPublisher` - Publishes events to Event Grid
2. `EventGridFunction<T>` - Base class for Event Grid triggered functions
3. Event serialization/deserialization helpers

## Implementation Approach

1. Create Event Grid publisher
2. Implement function bindings
3. Create base function classes
4. Add serialization support
5. Implement error handling and retries

## Container Security Events

Focus on these event types:
1. ContainerVulnerabilityEvent - Vulnerability detection
2. ProcessAnomalyEvent - Process-related anomalies
3. NetworkAnomalyEvent - Network-related anomalies