# Trickle Platform Refactoring Plan

## Overview

This document outlines the step-by-step approach to refactoring the Trickle security platform. The goal is to transform the existing codebase into a clean, maintainable architecture that follows modern best practices and supports the platform's scaling requirements.

## Branching Strategy

Each refactoring step will be implemented in a separate branch:

1. `refactor/step1-project-structure` - Reorganize project structure
2. `refactor/step2-postgres-repositories` - Implement Dapper-based repositories
3. `refactor/step3-reference-data` - Build reference data framework
4. `refactor/step4-event-grid` - Integrate Event Grid
5. `refactor/step5-container-security` - Implement Container Security domain

Each branch will be merged to main before starting the next step to ensure incremental progress.

## Step 1: Project Structure Refactoring

**Focus**: Establish the new architecture's foundation with proper separation of concerns.

**Key Deliverables**:
- Project structure under `/src2`
- Solution organization
- Core interfaces and abstractions
- Base classes for components

**Success Criteria**:
- Clean separation between components
- Clear domain boundaries
- Support for regional vs. central deployment model

## Step 2: PostgreSQL Repository Implementation

**Focus**: Build efficient data access layer with proper multi-tenancy.

**Key Deliverables**:
- Dapper implementation
- Connection management
- Schema design
- Domain repositories
- Resilience patterns

**Success Criteria**:
- Performant data access
- Developer-friendly APIs
- Strong typing with minimal boilerplate
- Proper error handling and retries

## Step 3: Reference Data Framework

**Focus**: Create a standalone reference data subsystem.

**Key Deliverables**:
- Reference repositories
- Caching mechanism
- Update orchestration
- ADX synchronization
- Container Security references

**Success Criteria**:
- Clear separation from event processing
- Efficient caching
- Consistent update patterns
- Proper synchronization with ADX

## Step 4: Event Grid Integration

**Focus**: Replace Service Bus with Event Grid for messaging.

**Key Deliverables**:
- Event Grid publisher
- Function triggers
- Event schema
- Routing patterns
- Error handling

**Success Criteria**:
- Clean messaging abstraction
- Proper multi-tenancy
- Efficient routing
- Resilient processing

## Step 5: Container Security Implementation

**Focus**: Build a complete vertical slice for the Container Security domain.

**Key Deliverables**:
- StackRox integration
- Vulnerability management
- Process alerts
- Network alerts
- End-to-end flows

**Success Criteria**:
- Complete working vertical slice
- Integration of all components
- Proper error handling
- Clear extension points

## Testing Strategy

Each step should include appropriate testing:

1. Unit tests for domain logic
2. Integration tests for data access
3. End-to-end tests for complete flows

## Migration Strategy

Once the refactored platform is ready:

1. Run parallel with existing implementation
2. Migrate one security domain at a time
3. Validate functionality
4. Gradually decommission old components