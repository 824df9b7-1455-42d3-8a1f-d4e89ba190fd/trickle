Initial Instructions:
- Replace the existing Service Bus implementation with Event Grid
- Implement a clean publisher/subscriber pattern for security events
- Ensure proper multi-tenancy and event routing
- Integrate with Azure Functions isolated model
- Demonstrate with Container Security domain events

Important Context:
- Event Grid will be our primary messaging system
- We need to maintain tenant isolation in event routing
- Error handling and retry policies are critical
- The implementation should support high throughput
- Events must be properly typed and versioned