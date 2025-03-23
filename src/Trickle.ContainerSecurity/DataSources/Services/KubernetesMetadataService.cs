using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ContainerService;
using Microsoft.Azure.Management.ContainerService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Trickle.Common.Dimensions;
using Trickle.ContainerSecurity.DataSources.Services;

namespace Trickle.ContainerSecurity.DataSources.Services
{
    /// <summary>
    /// Service for retrieving Kubernetes cluster metadata from Azure
    /// </summary>
    public class KubernetesMetadataService
    {
        private readonly ILogger<KubernetesMetadataService> _logger;
        private readonly IDimension<AzureSubscription> _subscriptionDimension;
        private readonly TokenCredentials _tokenCredentials;
        
        public KubernetesMetadataService(
            ILogger<KubernetesMetadataService> logger,
            IDimension<AzureSubscription> subscriptionDimension,
            TokenCredentials tokenCredentials)
        {
            _logger = logger;
            _subscriptionDimension = subscriptionDimension;
            _tokenCredentials = tokenCredentials;
        }
        
        /// <summary>
        /// Get all AKS clusters across subscriptions
        /// </summary>
        public async Task<List<AksClusterInfo>> GetAksClustersAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<AksClusterInfo>();
            var subscriptions = await _subscriptionDimension.GetAllAsync();
            
            foreach (var subscription in subscriptions)
            {
                try
                {
                    var subscriptionClusters = await GetAksClustersForSubscriptionAsync(subscription.SubscriptionId, cancellationToken);
                    results.AddRange(subscriptionClusters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting AKS clusters for subscription {SubscriptionId}", subscription.SubscriptionId);
                    // Continue with other subscriptions
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Get AKS clusters for a specific subscription
        /// </summary>
        public async Task<List<AksClusterInfo>> GetAksClustersForSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting AKS clusters for subscription {SubscriptionId}", subscriptionId);
                
                using var aksClient = new ContainerServiceClient(_tokenCredentials)
                {
                    SubscriptionId = subscriptionId
                };
                
                var clusterPages = await aksClient.ManagedClusters.ListAsync(cancellationToken: cancellationToken);
                var results = new List<AksClusterInfo>();
                
                // Process first page
                foreach (var cluster in clusterPages)
                {
                    results.Add(MapToClusterInfo(cluster, subscriptionId));
                }
                
                // Process remaining pages if any
                string nextPageLink = clusterPages.NextPageLink;
                while (!string.IsNullOrEmpty(nextPageLink))
                {
                    var nextPage = await aksClient.ManagedClusters.ListNextAsync(nextPageLink, cancellationToken);
                    foreach (var cluster in nextPage)
                    {
                        results.Add(MapToClusterInfo(cluster, subscriptionId));
                    }
                    nextPageLink = nextPage.NextPageLink;
                }
                
                _logger.LogInformation("Found {ClusterCount} AKS clusters in subscription {SubscriptionId}", 
                    results.Count, subscriptionId);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AKS clusters for subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }
        
        /// <summary>
        /// Map Azure SDK model to our domain model
        /// </summary>
        private AksClusterInfo MapToClusterInfo(ManagedCluster cluster, string subscriptionId)
        {
            return new AksClusterInfo
            {
                ResourceId = cluster.Id,
                Name = cluster.Name,
                SubscriptionId = subscriptionId,
                ResourceGroupName = GetResourceGroupFromId(cluster.Id),
                Location = cluster.Location,
                KubernetesVersion = cluster.KubernetesVersion,
                Status = cluster.ProvisioningState,
                NodeResourceGroupName = cluster.NodeResourceGroup,
                Tags = cluster.Tags,
                NetworkProfile = new AksNetworkProfile
                {
                    NetworkPlugin = cluster.NetworkProfile?.NetworkPlugin,
                    NetworkPolicy = cluster.NetworkProfile?.NetworkPolicy,
                    PodCidr = cluster.NetworkProfile?.PodCidr,
                    ServiceCidr = cluster.NetworkProfile?.ServiceCidr
                },
                AgentPoolProfiles = cluster.AgentPoolProfiles?.Select(p => new AksAgentPoolProfile
                {
                    Name = p.Name,
                    Count = p.Count ?? 0,
                    VmSize = p.VmSize,
                    OsType = p.OsType,
                    OsDiskSizeGB = p.OsDiskSizeGB ?? 0
                }).ToList() ?? new List<AksAgentPoolProfile>()
            };
        }
        
        /// <summary>
        /// Extract resource group name from resource ID
        /// </summary>
        private string GetResourceGroupFromId(string resourceId)
        {
            // Format: /subscriptions/{subId}/resourceGroups/{rgName}/providers/...
            var parts = resourceId.Split('/');
            if (parts.Length >= 5 && parts[3].Equals("resourceGroups", StringComparison.OrdinalIgnoreCase))
            {
                return parts[4];
            }
            return null;
        }
    }
    
    /// <summary>
    /// Azure subscription information
    /// </summary>
    public class AzureSubscription
    {
        /// <summary>
        /// Subscription ID
        /// </summary>
        public string SubscriptionId { get; set; }
        
        /// <summary>
        /// Subscription display name
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Subscription state
        /// </summary>
        public string State { get; set; }
        
        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; }
    }
    
    /// <summary>
    /// AKS cluster information
    /// </summary>
    public class AksClusterInfo
    {
        /// <summary>
        /// Full Azure resource ID
        /// </summary>
        public string ResourceId { get; set; }
        
        /// <summary>
        /// Cluster name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Subscription ID
        /// </summary>
        public string SubscriptionId { get; set; }
        
        /// <summary>
        /// Resource group name
        /// </summary>
        public string ResourceGroupName { get; set; }
        
        /// <summary>
        /// Azure region
        /// </summary>
        public string Location { get; set; }
        
        /// <summary>
        /// Kubernetes version
        /// </summary>
        public string KubernetesVersion { get; set; }
        
        /// <summary>
        /// Provisioning state
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// Node resource group name
        /// </summary>
        public string NodeResourceGroupName { get; set; }
        
        /// <summary>
        /// Azure tags
        /// </summary>
        public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Network configuration
        /// </summary>
        public AksNetworkProfile NetworkProfile { get; set; }
        
        /// <summary>
        /// Node pools
        /// </summary>
        public List<AksAgentPoolProfile> AgentPoolProfiles { get; set; } = new List<AksAgentPoolProfile>();
    }
    
    /// <summary>
    /// AKS network profile
    /// </summary>
    public class AksNetworkProfile
    {
        /// <summary>
        /// Network plugin (e.g., "azure", "kubenet")
        /// </summary>
        public string NetworkPlugin { get; set; }
        
        /// <summary>
        /// Network policy (e.g., "calico")
        /// </summary>
        public string NetworkPolicy { get; set; }
        
        /// <summary>
        /// Pod CIDR range
        /// </summary>
        public string PodCidr { get; set; }
        
        /// <summary>
        /// Service CIDR range
        /// </summary>
        public string ServiceCidr { get; set; }
    }
    
    /// <summary>
    /// AKS agent pool profile
    /// </summary>
    public class AksAgentPoolProfile
    {
        /// <summary>
        /// Pool name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Number of nodes
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// VM size
        /// </summary>
        public string VmSize { get; set; }
        
        /// <summary>
        /// OS type
        /// </summary>
        public string OsType { get; set; }
        
        /// <summary>
        /// OS disk size in GB
        /// </summary>
        public int OsDiskSizeGB { get; set; }
    }
}
