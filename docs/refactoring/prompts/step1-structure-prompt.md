# Step 1: Project Structure Refactoring Prompt

I need to reorganize the Trickle security platform project structure to better support our architectural patterns. We're splitting components into collectors (regional), analyzers (central), and responders (central), with a strong focus on Container Security domain. Help me implement the new project structure under /src2, providing a clear folder hierarchy, essential .csproj files, and key shared files.

The goals are:
- Establish clean separation between components
- Support regional vs. central deployment model
- Provide clear domain boundaries
- Enable proper reference sharing

The high-level architecture involves:
1. Collectors: Poll external APIs or consume event streams (regional deployment)
2. Analyzers: Process domain events with business logic (central deployment)
3. Responders: React to security events with notifications (central deployment)
4. Reference Data: Security context information (shared across components)

For this restructuring:
1. Focus on Container Security domain for the reference implementation
2. Ensure proper project references between components
3. Create essential interfaces and abstractions
4. Set up the solution structure with appropriate project grouping

I need a complete implementation with:
- New folder structure under /src2
- Project files (.csproj) with appropriate references
- Solution file (.sln) organization
- Key shared interfaces and base classes