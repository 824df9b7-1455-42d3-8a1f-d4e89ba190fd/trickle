# Step 2: PostgreSQL Repository Pattern Implementation Prompt

I need to implement a clean Dapper-based repository pattern for the Trickle platform's PostgreSQL integration. This should support state management for security events with appropriate schema separation between domains. Focus on making a developer-friendly API that mid-level engineers can easily use while maintaining performance and scalability. Start with the Container Security domain for vulnerability state management.

The requirements include:
- Schema design with trickle_{domain}_{purpose} naming convention
- Dapper implementation with proper connection management
- Support for multi-tenancy and security boundaries
- Resilience patterns (retries, circuit breakers)
- Developer-friendly repository interfaces
- Performance optimization techniques

For the Container Security domain, implement repositories for:
1. VulnerableCluster - Clusters with vulnerabilities
2. ContainerVulnerability - Specific vulnerabilities
3. ProcessAnomaly - Process-related security events
4. NetworkAnomaly - Network-related security events

Include:
- Connection factory with resilience
- Base repository with Dapper implementation
- Domain-specific repositories
- Transaction support
- Integration with telemetry
- SQL helpers for complex queries
- Migration framework setup

Ensure the code is production-quality with proper error handling, logging, and documentation.