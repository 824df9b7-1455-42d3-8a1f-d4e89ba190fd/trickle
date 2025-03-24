# Step 3: Reference Data Framework Implementation Prompt

I need to implement a Reference Data framework for the Trickle security platform that's separate from our event processing pipeline. This should allow us to manage security context data like cluster information, vulnerability definitions, and allowlists. Focus on the Container Security domain and show how reference data updaters would work with external sources like StackRox API.

The requirements include:
- Separation of reference data from event processing
- Caching mechanism with appropriate refresh patterns
- Support for both PostgreSQL and ADX persistence
- Clean updater pattern for refreshing reference data
- Multi-tenancy support

For the Container Security domain, implement these reference entities:
1. KubernetesCluster - Cluster information
2. CveAllowlist - CVE exception lists
3. VulnerabilityDefinition - Known vulnerability data

Include:
- Reference data abstractions
- PostgreSQL repositories for reference data
- Caching mechanism
- Update orchestration
- ADX synchronization for reporting
- Example reference data updaters for each entity

Ensure the framework is extensible and provides clear patterns for adding new reference data types.