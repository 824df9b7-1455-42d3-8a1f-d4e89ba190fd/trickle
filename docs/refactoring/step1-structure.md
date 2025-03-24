# Step 1: Project Structure Refactoring

## Branch: `refactor/step1-project-structure`

## Goals
- Establish clean separation between components
- Support regional vs. central deployment model
- Provide clear domain boundaries
- Enable proper reference sharing

## Key Interfaces to Implement

1. Base events and models
2. Event Grid publisher/subscriber
3. Repository interfaces
4. Reference data abstractions
5. Component base classes

## Implementation Approach

1. Start with `Trickle.Common` for core abstractions
2. Create domain models for Container Security
3. Set up project structure with proper references
4. Implement skeleton classes for key interfaces

## Project Structure
src2/
├── Trickle.Common/               # Shared core abstractions
│   ├── Domain/                   # Base domain models
│   ├── EventGrid/                # Event Grid integration
│   ├── Persistence/              # Database abstractions
│   └── Telemetry/                # Logging and monitoring
│
├── Trickle.References/           # Reference data framework
│   ├── Common/                   # Core reference abstractions
│   ├── Repositories/             # Reference data storage
│   ├── Services/                 # Reference data services
│   └── Updaters/                 # Reference data updaters
│
├── Security Domains/
│   ├── Trickle.ContainerSecurity.Core/  # Container domain models
│   ├── Trickle.NetworkSecurity.Core/    # Network domain models
│   └── Trickle.IdentitySecurity.Core/   # Identity domain models
│
├── Collectors/                   # Regional deployments
│   ├── Trickle.Collectors.Container/  # Container security collectors
│   ├── Trickle.Collectors.Network/    # Network security collectors
│   └── Trickle.Collectors.Identity/   # Identity security collectors
│
├── Analyzers/                    # Central deployments
│   └── Trickle.Analyzers/        # All domain analyzers
│
└── Responders/                   # Central deployments
└── Trickle.Responders/       # All domain responders