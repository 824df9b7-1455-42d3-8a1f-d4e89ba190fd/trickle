# Trickle Platform Architecture Overview

## Core Components

### Collectors
- Poll external APIs or consume event streams
- Standardize events and publish to Event Grid
- Regional deployment for proximity to sources
- Owned by security domain teams (Container, Network, Identity)

### Analyzers
- Process domain events with business logic
- Enrich and correlate security events
- Maintain state in PostgreSQL 
- Centralized deployment
- Cross-domain correlation

### Responders
- React to security events with notifications and actions
- Integrate with external systems (Teams, Email, JIRA)
- Template-driven for customization
- Centralized deployment

### Reference Data
- Security context information
- External authoritative sources
- Used by all components for enrichment
- PostgreSQL storage with ADX synchronization

## Data Flow

1. External sources -> Collectors -> Event Grid
2. Event Grid -> Analyzers -> State updates
3. Analyzers -> Event Grid -> Responders
4. Responders -> Notification destinations

## Key Patterns

1. **Multi-tenancy**: Tenant isolation across components
2. **Event-driven**: Asynchronous processing via Event Grid
3. **State management**: PostgreSQL for operational state
4. **Reference data**: Cached security context information
5. **Domain separation**: Vertical slices by security domain

## Technology Stack

- .NET 7 / C# 10
- Azure Functions v4 (isolated process model)
- Azure Event Grid
- PostgreSQL
- Azure Data Explorer (ADX)
- Dapper for data access