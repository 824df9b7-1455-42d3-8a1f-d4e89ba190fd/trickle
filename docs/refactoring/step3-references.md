# Step 3: Reference Data Framework

## Branch: `refactor/step3-reference-data`

## Goals
- Separate reference data from event processing
- Implement caching with appropriate refresh
- Support both PostgreSQL and ADX persistence
- Create clean updater patterns

## Key Concepts

### Reference Data Types
1. **Static References** - Configuration-driven (allowlists, thresholds)
2. **Dynamic References** - Externally sourced (clusters, namespaces)
3. **Derived References** - Calculated from events (vulnerability summaries)

### Repository Structure
- PostgreSQL as primary storage
- ADX synchronized for query performance
- In-memory cache for high-throughput scenarios

## Core Interfaces

1. `IReferenceRepository<T>` - Base reference repository
2. `IReferenceUpdater<T>` - Updates reference data
3. `IReferenceCache<T>` - Caching abstraction

## Implementation Approach

1. Create reference data abstractions
2. Implement PostgreSQL repositories
3. Add caching mechanism
4. Create updater framework
5. Implement ADX synchronization

## Container Security References

Focus on these reference entities:
1. KubernetesCluster - Cluster information
2. CveAllowlist - CVE exception lists
3. VulnerabilityDefinition - Known vulnerability data