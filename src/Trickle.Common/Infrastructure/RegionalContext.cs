using System;
using System.Collections.Generic;
using System.Linq;

namespace Trickle.Common.Infrastructure
{
    /// <summary>
    /// Provides regional context for functions deployed regionally
    /// </summary>
    public class RegionalContext
    {
        /// <summary>
        /// Flag indicating if this instance is region-specific
        /// </summary>
        public bool IsRegionalInstance { get; set; }
        
        /// <summary>
        /// Azure region for this instance (e.g., "eastus", "westeurope")
        /// </summary>
        public string Region { get; set; }
        
        /// <summary>
        /// Azure subscriptions supported by this regional instance
        /// </summary>
        public string[] SupportedSubscriptions { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// Flag indicating if this instance is the global coordinator
        /// </summary>
        public bool IsGlobalCoordinator { get; set; }
        
        /// <summary>
        /// Additional regional configuration properties
        /// </summary>
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Check if a resource is in the regional scope of this instance
        /// </summary>
        public bool IsInRegionalScope(string subscriptionId, string location = null)
        {
            // If this is not a regional instance, everything is in scope
            if (!IsRegionalInstance)
                return true;
                
            // If location is provided and matches this region, it's in scope
            if (!string.IsNullOrEmpty(location) && 
                location.Equals(Region, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Check if the subscription is in the supported list
            if (!string.IsNullOrEmpty(subscriptionId) && 
                SupportedSubscriptions.Contains(subscriptionId, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Not in this region's scope
            return false;
        }
        
        /// <summary>
        /// Check if a resource ID is in the regional scope of this instance
        /// </summary>
        public bool IsResourceIdInRegionalScope(string resourceId)
        {
            // If this is not a regional instance, everything is in scope
            if (!IsRegionalInstance)
                return true;
                
            // Extract subscription ID from resource ID
            // Format: /subscriptions/{subId}/resourceGroups/{rgName}/...
            var parts = resourceId?.Split('/');
            if (parts == null || parts.Length < 3 || 
                !parts[1].Equals("subscriptions", StringComparison.OrdinalIgnoreCase))
            {
                // Not a valid Azure resource ID
                return false;
            }
            
            var subscriptionId = parts[2];
            
            // Check if the subscription is in the supported list
            return SupportedSubscriptions.Contains(subscriptionId, StringComparer.OrdinalIgnoreCase);
        }
    }
}
