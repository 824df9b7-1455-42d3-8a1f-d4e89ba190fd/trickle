# 5. Regional Deployment Model

## Status

Accepted

## Context

The Trickle platform needs to efficiently collect security data from various sources across different Azure regions. We need to decide on the deployment model that provides the right balance of performance, cost, and operational complexity.

Options considered:
1. **Fully Regional**: Deploy all components (Collectors, Analyzers, Responders) in each region
2. **Fully Centralized**: Deploy all components in a single region
3. **Hybrid Model**: Deploy Collectors regionally, Analyzers and Responders centrally

## Decision

We will implement a hybrid regional deployment model:

1. **Collectors**: Deployed regionally
   - Close to data sources for efficient collection
   - Reduced egress costs for high-volume data
   - Independent scaling based on regional load

2. **Analyzers and Responders**: Deployed centrally
   - Centralized analysis for cross-region correlation
   - Simplified state management
   - Lower operational overhead

3. **Reference Data**: Globally accessible
   - Read replicas in each region
   - Central write operations
   - Caching for performance

4. **Event Grid**: Global service with regional topics
   - Regional topics for collector events
   - Central topics for analyzer events
   - Proper routing between regions

## Consequences

### Positive
- Efficient data collection close to sources
- Reduced data egress costs
- Simplified central analysis
- Cross-region correlation capabilities
- Clear deployment boundaries

### Negative
- More complex deployment model
- Cross-region event routing overhead
- Need for robust regional health monitoring
- Potential for region-specific issues
- More complex CI/CD pipelines

## Related ADRs

- ADR-0001: Event Grid Messaging
- ADR-0004: Reference Data Framework