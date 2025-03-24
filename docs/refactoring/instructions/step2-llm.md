Initial Instructions:
- Implement a clean Dapper-based repository pattern optimized for PostgreSQL
- Focus on making it both performant and easy to use for mid-level developers
- Include proper resilience patterns (retries, circuit breakers)
- Support multi-tenancy for all state data
- Provide concrete implementations for the Container Security domain

Important Context:
- We're using PostgreSQL for operational state, not for event storage
- The databases will be heavily used for reads and writes
- Security teams will extend this pattern for new security domains
- We need to support row-level security for multi-tenancy
- The implementation should include migration support