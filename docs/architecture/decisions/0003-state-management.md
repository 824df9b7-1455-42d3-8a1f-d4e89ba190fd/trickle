# 3. State Management with PostgreSQL

## Status

Accepted

## Context

The Trickle platform needs a reliable state management solution for operational data. While events are stored in Azure Data Explorer (ADX) for analytical purposes, we need a transactional database for mutable state and reference data.

Options considered:
1. **Azure SQL**: Managed SQL Server with familiar SQL dialect
2. **PostgreSQL**: Open-source RDBMS with JSONB and other advanced features
3. **Cosmos DB**: NoSQL document database with global distribution
4. **Azure Table Storage**: Simple key-value storage

## Decision

We will use Azure Database for PostgreSQL Flexible Server as our state management database with the following characteristics:

1. **Schema Design**:
   - Schema per domain and purpose: `trickle_{domain}_{purpose}`
   - Standard tables with strongly-typed columns for core fields
   - JSONB columns for flexible, schema-less properties

2. **Access Pattern**:
   - Dapper as the data access library
   - Repository pattern with strongly-typed entities
   - Connection management with retry policies

3. **Multi-Tenancy**:
   - Row-level security policies
   - Tenant context set via session variables
   - Tenant ID column on all tables

4. **Performance**:
   - Appropriate indexing strategy
   - Materialized views for complex aggregations
   - Connection pooling

## Consequences

### Positive
- JSONB support provides flexibility for evolving data models
- Strong typing for core fields ensures data integrity
- Dapper offers high performance with minimal overhead
- PostgreSQL's advanced features support our complex queries
- Row-level security provides strong multi-tenancy isolation

### Negative
- Need for PostgreSQL-specific expertise
- Potential for schema evolution challenges
- Connection management complexity in serverless environments
- Need to manage write conflicts in high-concurrency scenarios

## Related ADRs

- ADR-0002: Multi-Tenancy Implementation
- ADR-0004: Reference Data Framework