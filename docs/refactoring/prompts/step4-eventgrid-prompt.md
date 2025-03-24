# Step 4: Event Grid Integration Implementation Prompt

I need to replace the existing Service Bus implementation with Event Grid in the Trickle security platform. Focus on implementing a clean publisher/subscriber pattern for security events that maintains multi-tenancy, supports proper scaling, and integrates well with Azure Functions. Demonstrate the pattern with the Container Security domain's vulnerability events.

The requirements include:
- Event Grid publisher implementation
- Function triggers for Event Grid events
- Event schema design with proper subject naming
- Tenant-aware routing
- Error handling and retry policies
- Integration with reference data

For the Container Security domain, implement these event types:
1. ContainerVulnerabilityEvent - Vulnerability detection
2. ProcessAnomalyEvent - Process-related anomalies
3. NetworkAnomalyEvent - Network-related anomalies

Include:
- Event Grid publisher
- Event Grid triggered functions
- Base function classes for event handling
- Serialization/deserialization support
- Error handling and retry policies
- Integration with existing components

Ensure the implementation supports proper scaling and maintains multi-tenancy across all components.