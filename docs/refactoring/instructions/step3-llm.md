Initial Instructions:
- Implement a Reference Data framework that's separate from event processing
- Focus on caching, efficient updates, and PostgreSQL persistence
- Implement ADX synchronization for reporting purposes
- Provide concrete examples for the Container Security domain
- Show how reference data updaters work with external sources

Important Context:
- Reference data changes infrequently but is accessed very frequently
- Some reference data comes from external systems (like StackRox)
- Other reference data is configuration-driven (like allowlists)
- Reference data needs to be available in both PostgreSQL and ADX
- We need to maintain tenant isolation for sensitive reference data