Initial Instructions:
- Focus on creating a clean, maintainable project structure that aligns with our architectural vision
- Provide actual .csproj files with proper references, not just folder structures
- Include the key interfaces and base classes that will be shared across components
- Ensure the structure supports our regional/central deployment model
- Create the actual solution file structure

Important Context:
- The primary security domains are Container Security, Network Security, and Identity Security
- We need to separate collectors (regional) from analyzers and responders (central)
- Reference data is shared across all components
- The Container Security domain will be our reference implementation