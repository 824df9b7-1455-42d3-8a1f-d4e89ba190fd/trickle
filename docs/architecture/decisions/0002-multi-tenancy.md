# 2. Multi-Tenancy Implementation

## Status

Accepted

## Context

The Trickle platform needs to support multiple tenants (security teams) with proper isolation of data and processing. We need to determine the appropriate multi-tenancy model that provides the right balance of isolation, performance, and operational complexity.

Options considered:
1. **Database-per-tenant**: Separate PostgreSQL schema and ADX database for each tenant
2. **Shared database with row-level security**: Single database with tenant ID columns and row-level security
3. **Hybrid approach**: Shared PostgreSQL with row-level security, separate ADX databases

## Decision

We will implement a hybrid multi-tenancy approach:

1. **PostgreSQL**: Shared database with schema-per-domain and row-level security
   - Schema naming: `trickle_{domain}_{purpose}`
   - Row-level security policies for tenant isolation
   - Tenant context set on connection using session variables

2. **ADX**: Database-per-tenant
   - Database naming: `trickle_{tenant_id}`
   - Event Grid routing to tenant-specific topics
   - Query service that transparently routes to appropriate database

3. **Event Grid**: Subject-based isolation
   - Subject pattern: `/security/{domain}/{tenant_id}/{resource-type}`
   - Subscription filters based on tenant ID part of subject
   - Tenant ID included in event data

## Consequences

### Positive
- Strong isolation for analytical data in ADX
- Efficient resource sharing for PostgreSQL data
- Simplified operations with fewer PostgreSQL databases
- Row-level security enforcement at database level
- Clear separation between tenants in event routing

### Negative
- Complexity in managing ADX databases per tenant
- Need to ensure row-level security is properly implemented
- Potential for misconfiguration exposing data across tenants
- Additional overhead for session context management

## Related ADRs

- ADR-0001: Event Grid Messaging
- ADR-0003: State Management with PostgreSQL
- ADR-0004: Reference Data Framework