```mermaid
C4Container
  title Trickle Security Platform - Container Diagram
  
  Person(secOps, "Security Operations Team", "Monitors security events and responds to incidents")
  
  System_Boundary(trickle, "Trickle Security Platform") {
    Container(collectors, "Collectors", "Azure Functions", "Poll external APIs or consume event streams, deployed regionally")
    Container(analyzers, "Analyzers", "Azure Functions", "Process security events, apply correlation logic, maintain state")
    Container(responders, "Responders", "Azure Functions", "Format and surface events to security operators")
    Container(referenceData, "Reference Data", "Azure Functions", "Maintain reference data for security context")
    
    ContainerDb(postgres, "PostgreSQL", "Database", "Stores operational state and reference data")
    ContainerDb(adx, "Azure Data Explorer", "Database", "Stores security events for analysis")
    Container(eventGrid, "Azure Event Grid", "Messaging", "Handles event routing between components")
  }
  
  System_Ext(azureResources, "Azure Resources", "Cloud resources being monitored")
  System_Ext(stackRox, "StackRox", "Container security platform")
  System_Ext(cloudflare, "Cloudflare", "Edge security services")
  System_Ext(entraID, "Microsoft Entra ID", "Identity management platform")
  
  System_Ext(teams, "Microsoft Teams", "Collaboration platform")
  System_Ext(email, "Email System", "Notification delivery")
  System_Ext(jira, "JIRA", "Ticket management system")
  
  Rel(secOps, adx, "Views security reports in")
  Rel(secOps, referenceData, "Configures security rules in")
  
  Rel(collectors, azureResources, "Monitors")
  Rel(collectors, stackRox, "Collects data from")
  Rel(collectors, cloudflare, "Retrieves metrics from")
  Rel(collectors, entraID, "Monitors identity in")
  
  Rel(collectors, eventGrid, "Publishes events to")
  Rel(eventGrid, analyzers, "Triggers")
  Rel(analyzers, postgres, "Maintains state in")
  Rel(analyzers, eventGrid, "Publishes notifications to")
  Rel(eventGrid, responders, "Triggers")
  
  Rel(referenceData, postgres, "Stores reference data in")
  Rel(referenceData, adx, "Syncs reference data to")
  Rel(analyzers, adx, "Persists events to")
  
  Rel(responders, teams, "Sends alerts to")
  Rel(responders, email, "Sends notifications via")
  Rel(responders, jira, "Creates tickets in")
  
  UpdateRelStyle(secOps, adx, $textColor="green", $lineColor="green")
  UpdateRelStyle(secOps, referenceData, $textColor="green", $lineColor="green")
  UpdateRelStyle(responders, teams, $textColor="red", $lineColor="red")
  UpdateRelStyle(responders, email, $textColor="red", $lineColor="red")
  UpdateRelStyle(responders, jira, $textColor="red", $lineColor="red")
  ```