# 1. Use Azure Event Grid for Event Messaging

## Status

Accepted

## Context

The Trickle platform needs a reliable messaging system to handle asynchronous communication between components (Collectors, Analyzers, and Responders). We initially implemented this using Azure Service Bus, but need to reconsider this choice for better alignment with our architectural goals.

Key requirements for our messaging system:
- Support for high-volume event publishing
- Topic-based routing
- Multi-tenant isolation
- Integration with Azure Functions
- Cost-effective at scale
- Minimal operational overhead

## Decision

We will use Azure Event Grid as the primary messaging system for the Trickle platform, replacing the current Azure Service Bus implementation.

This decision is based on the following factors:
1. **Event-Driven Architecture**: Event Grid is purpose-built for event-driven architectures, which aligns with our platform design.
2. **Integration**: Native integration with Azure Functions and other Azure services.
3. **Routing Capabilities**: Event Grid's subject and type-based filtering provides the routing flexibility we need.
4. **Cost Model**: Event Grid's per-event pricing is more cost-effective for our usage patterns.
5. **Managed Service**: Fully managed with minimal operational overhead.
6. **Scalability**: Designed for high-volume event publishing.

## Consequences

### Positive
- Better alignment with event-driven architecture patterns
- Simplified routing using subject patterns and event types
- Native integration with Azure Functions trigger bindings
- Potential cost savings for our event volume
- Reduced operational overhead

### Negative
- Requires refactoring existing Service Bus-based components
- Limited ordered delivery guarantees compared to Service Bus (not a critical requirement for us)
- Less mature ecosystem compared to Service Bus
- Requires implementation of a dead-letter handling approach

## Related ADRs

- ADR-0002: Multi-Tenancy Implementation
- ADR-0003: State Management with PostgreSQL