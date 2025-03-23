using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trickle.Common.Domain
{
    /// <summary>
    /// Base class for all security events in the Trickle platform.
    /// Provides common properties and behaviors for events across all security domains.
    /// </summary>
    public abstract class SecurityEvent
    {
        /// <summary>
        /// Unique identifier for this event
        /// </summary>
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of security event (used for routing and processing)
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// When the event was detected
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Business owner ID for tenant isolation and routing
        /// </summary>
        public string OwnerId { get; set; }
        
        /// <summary>
        /// Severity level of the security event
        /// </summary>
        public SecuritySeverity Severity { get; set; } = SecuritySeverity.Low;
        
        /// <summary>
        /// Resource identifier this event relates to (Azure Resource ID or similar)
        /// </summary>
        public string ResourceId { get; set; }
        
        /// <summary>
        /// Optional correlation ID for tracing related events
        /// </summary>
        public string CorrelationId { get; set; }
        
        /// <summary>
        /// Additional metadata as key-value pairs
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        protected SecurityEvent()
        {
            // Default event type to the concrete class name (e.g., "ExposedVmEvent")
            EventType = GetType().Name;
        }
        
        /// <summary>
        /// Validate the security event
        /// </summary>
        /// <returns>True if the event is valid; otherwise, false</returns>
        public virtual bool Validate(out List<string> validationErrors)
        {
            validationErrors = new List<string>();
            
            if (string.IsNullOrEmpty(EventId))
                validationErrors.Add("EventId is required");
                
            if (string.IsNullOrEmpty(EventType))
                validationErrors.Add("EventType is required");
                
            if (DetectedAt == default)
                validationErrors.Add("DetectedAt is required");
                
            if (string.IsNullOrEmpty(ResourceId))
                validationErrors.Add("ResourceId is required");
                
            return validationErrors.Count == 0;
        }
        
        /// <summary>
        /// Serialize the event to JSON
        /// </summary>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Serialize(this, GetType(), options);
        }
        
        /// <summary>
        /// Get properties for persisting to ADX
        /// </summary>
        public virtual Dictionary<string, object> GetAdxProperties()
        {
            return new Dictionary<string, object>
            {
                ["EventId"] = EventId,
                ["EventType"] = EventType,
                ["DetectedAt"] = DetectedAt,
                ["OwnerId"] = OwnerId,
                ["Severity"] = Severity.ToString(),
                ["ResourceId"] = ResourceId,
                ["CorrelationId"] = CorrelationId,
                ["RawEventData"] = ToJson()
            };
        }
    }
    
    /// <summary>
    /// Severity levels for security events
    /// </summary>
    public enum SecuritySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
