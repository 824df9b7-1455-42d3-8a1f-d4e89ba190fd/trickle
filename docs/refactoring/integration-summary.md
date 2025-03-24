# Trickle Platform Integration Summary

This document summarizes how the different components interact in the refactored architecture.

## Component Interactions

1. **Collectors -> Event Grid -> Analyzers**
   - Collectors publish domain events to Event Grid
   - Analyzers subscribe to relevant event types
   - Event Grid handles routing and filtering

2. **Analyzers -> PostgreSQL**
   - Analyzers maintain state in PostgreSQL
   - Dapper repositories provide efficient data access
   - Transactions ensure consistency

3. **Analyzers -> Event Grid -> Responders**
   - Analyzers generate enriched events
   - Responders trigger on specific event types
   - Notification templates determine formatting

4. **Reference Data -> All Components**
   - Reference repositories provide security context
   - Cached for performance
   - Updated by scheduled functions

## Integration Points

1. **Tenant Isolation**
   - Event Grid subject patterns
   - PostgreSQL schema/row-level security
   - ADX database per tenant

2. **Error Handling**
   - Consistent retry policies
   - Dead-letter handling
   - Telemetry integration

3. **Deployment Boundaries**
   - Regional collectors
   - Central analyzers/responders
   - Shared reference data

## Testing Strategy

1. Unit tests for domain logic
2. Integration tests for data access
3. End-to-end tests for full flows