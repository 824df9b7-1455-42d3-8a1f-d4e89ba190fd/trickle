# Step 2: PostgreSQL Repository Implementation

## Branch: `refactor/step2-postgres-repositories`

## Goals
- Implement efficient Dapper-based repositories
- Establish clear schema separation
- Support multi-tenancy and security boundaries
- Create developer-friendly APIs

## Schema Design

### Naming Convention
trickle_{domain}_{purpose}
Example schemas:
- `trickle_container_state` - Container security state
- `trickle_container_references` - Container security reference data
- `trickle_network_state` - Network security state
- `trickle_common_references` - Shared reference data

### Table Design Guidelines
- Use snake_case for table and column names
- Include standard audit columns (created_at, updated_at, etc.)
- Use tenant_id column for multi-tenancy
- Implement proper indexing strategy
- Consider JSONB for flexible properties

## Repository Pattern

### Key Interfaces

1. `IDbConnectionFactory` - Creates database connections
2. `IRepository<T>` - Generic repository operations
3. Domain-specific repositories for specialized queries

### Implementation Approach

1. Implement connection factory with proper resilience
2. Create base repository with Dapper implementation
3. Add domain-specific repositories
4. Implement transaction support
5. Add integration with telemetry

## Example Implementation for Container Security

Focus on these state entities:
1. VulnerableCluster - Clusters with vulnerabilities
2. ContainerVulnerability - Specific vulnerabilities
3. ProcessAnomaly - Process-related security events