# 4. Reference Data Framework

## Status

Accepted

## Context

The Trickle platform requires reference data (like cluster information, CVE definitions, and allowlists) that is used to enrich and contextualize security events. This reference data has different characteristics from security events:
- Lower volume, higher stability
- Higher read-to-write ratio
- Used across multiple components
- Some is derived from external sources, some is configuration-driven

We need to decide how to manage this reference data within our architecture.

## Decision

We will implement a dedicated Reference Data Framework with the following characteristics:

1. **Separation from Event Processing**:
   - Dedicated components for reference data management
   - Clear separation from security event processing
   - Explicit reference repositories for consuming reference data

2. **Storage Strategy**:
   - PostgreSQL as the primary storage
   - In-memory caching for high-throughput scenarios
   - ADX synchronization for reporting purposes

3. **Update Patterns**:
   - Scheduled updaters for external reference data
   - Configuration-driven updates for static reference data
   - Clear versioning for reference data changes

4. **Consumption Patterns**:
   - Strongly-typed reference repositories
   - Caching with appropriate TTL
   - Efficient querying patterns

## Consequences

### Positive
- Clear separation of concerns
- High-performance access to reference data
- Consistent update patterns
- Reliable source of truth for security context
- ADX availability for reporting queries

### Negative
- Need to maintain synchronization between PostgreSQL and ADX
- Potential for stale cache data
- Added complexity for reference data management
- Need for careful cache invalidation strategies

## Related ADRs

- ADR-0002: Multi-Tenancy Implementation
- ADR-0003: State Management with PostgreSQL