# Step 5: Container Security Vertical Slice Implementation Prompt

Help me implement the complete Container Security vertical slice for the Trickle platform, integrating all the components we've built (project structure, PostgreSQL repositories, reference data, Event Grid). Focus on three key scenarios: vulnerability management, process alerts from StackRox, and network alerts from StackRox. Show the full flow from collector to analyzer to responder.

The scenarios include:
1. Vulnerability Management:
   - Detect vulnerabilities from StackRox
   - Track vulnerability state over time
   - Generate notifications for critical issues

2. Process Alerts:
   - Monitor container process anomalies
   - Detect suspicious processes
   - Correlate with known patterns

3. Network Alerts:
   - Monitor container network activity
   - Detect unusual connection patterns
   - Identify potential data exfiltration

Include:
- StackRox integration service
- Vulnerability collector implementation
- Process alert collector implementation
- Network alert collector implementation
- Corresponding analyzers for each collector
- Responder implementation for notifications
- Complete end-to-end flow

Demonstrate how all the components interact while maintaining separation of concerns and ensuring proper error handling and resilience.