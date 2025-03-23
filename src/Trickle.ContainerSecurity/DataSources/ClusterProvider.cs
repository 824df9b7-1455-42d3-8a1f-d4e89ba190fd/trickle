using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trickle.Common.DataSources;
using Trickle.Common.Dimensions;
using Trickle.ContainerSecurity.DataSources.Services;

namespace Trickle.ContainerSecurity.DataSources.Dimensions
{
    /// <summary>
    /// Provider for Kubernetes cluster information, combining StackRox and Azure data
    /// </summary>
    public class ClusterProvider : BaseDataSource, IDimensionSource<ClusterInfo>
    {
        private readonly ILogger<ClusterProvider> _logger;
        private readonly StackRoxClient _stackRoxClient;
        private readonly KubernetesMetadataService _kubernetesMetadataService;
        
        public ClusterProvider(
            ILogger<ClusterProvider> logger,
            StackRoxClient stackRoxClient,
            KubernetesMetadataService kubernetesMetadataService)
        {
            _logger = logger;
            _stackRoxClient = stackRoxClient;
            _kubernetesMetadataService = kubernetesMetadataService;
        }
        
        /// <summary>
        /// Get current cluster information
        /// </summary>
        public async Task<IReadOnlyList<ClusterInfo>> GetCurrentDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Collecting Kubernetes cluster information");
                
                // Get clusters from StackRox
                var stackRoxClusters = await _stackRoxClient.GetClustersAsync(cancellationToken);
                
                // Get clusters from Azure
                var azureClusters = await _kubernetesMetadataService.GetAksClustersAsync(cancellationToken);
                
                // Merge the data
                var mergedClusters = MergeClusters(stackRoxClusters, azureClusters);
                
                _logger.LogInformation("Collected information for {ClusterCount} Kubernetes clusters", mergedClusters.Count);
                
                // Track the refresh
                TrackRefresh(mergedClusters.Count, TimeSpan.Zero);
                
                return mergedClusters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting Kubernetes cluster information");
                throw;
            }
        }
        
        /// <summary>
        /// Merge cluster data from StackRox and Azure
        /// </summary>
        private List<ClusterInfo> MergeClusters(List<KubernetesCluster> stackRoxClusters, List<AksClusterInfo> azureClusters)
        {
            var results = new List<ClusterInfo>();
            
            // Map StackRox clusters
            foreach (var srcCluster in stackRoxClusters)
            {
                var clusterInfo = new ClusterInfo
                {
                    ClusterId = srcCluster.Id,
                    Name = srcCluster.Name,
                    Status = srcCluster.Status,
                    KubernetesVersion = srcCluster.Version,
                    Labels = new Dictionary<string, string>(srcCluster.Labels),
                    StackRoxIntegrated = true
                };
                
                // Try to find matching Azure cluster
                var matchingAzureCluster = FindMatchingAzureCluster(srcCluster, azureClusters);
                if (matchingAzureCluster != null)
                {
                    EnrichWithAzureData(clusterInfo, matchingAzureCluster);
                }
                
                results.Add(clusterInfo);
            }
            
            // Add Azure clusters that don't have StackRox integration
            foreach (var aksCluster in azureClusters)
            {
                // Skip if already processed via StackRox match
                if (results.Any(c => c.ResourceId == aksCluster.ResourceId))
                {
                    continue;
                }
                
                var clusterInfo = new ClusterInfo
                {
                    Name = aksCluster.Name,
                    Status = aksCluster.Status,
                    KubernetesVersion = aksCluster.KubernetesVersion,
                    StackRoxIntegrated = false
                };
                
                EnrichWithAzureData(clusterInfo, aksCluster);
                
                results.Add(clusterInfo);
            }
            
            return results;
        }
        
        /// <summary>
        /// Find an Azure cluster that matches a StackRox cluster
        /// </summary>
        private AksClusterInfo FindMatchingAzureCluster(KubernetesCluster stackRoxCluster, List<AksClusterInfo> azureClusters)
        {
            // Try to match by name or by labels
            
            // Check name match
            var nameMatch = azureClusters.FirstOrDefault(a => 
                a.Name.Equals(stackRoxCluster.Name, StringComparison.OrdinalIgnoreCase));
            if (nameMatch != null)
                return nameMatch;
                
            // Check for StackRox label with Azure resource ID
            if (stackRoxCluster.Labels != null && stackRoxCluster.Labels.TryGetValue("azure-resource-id", out var resourceId))
            {
                var labelMatch = azureClusters.FirstOrDefault(a => 
                    a.ResourceId.Equals(resourceId, StringComparison.OrdinalIgnoreCase));
                if (labelMatch != null)
                    return labelMatch;
            }
            
            return null;
        }
        
        /// <summary>
        /// Enrich cluster info with Azure data
        /// </summary>
        private void EnrichWithAzureData(ClusterInfo clusterInfo, AksClusterInfo aksCluster)
        {
            clusterInfo.ResourceId = aksCluster.ResourceId;
            clusterInfo.SubscriptionId = aksCluster.SubscriptionId;
            clusterInfo.ResourceGroupName = aksCluster.ResourceGroupName;
            clusterInfo.Location = aksCluster.Location;
            clusterInfo.NodeResourceGroupName = aksCluster.NodeResourceGroupName;
            
            // Merge tags
            if (aksCluster.Tags != null)
            {
                foreach (var tag in aksCluster.Tags)
                {
                    if (!clusterInfo.Labels.ContainsKey(tag.Key))
                    {
                        clusterInfo.Labels[tag.Key] = tag.Value;
                    }
                }
            }
            
            // Add network profile
            if (aksCluster.NetworkProfile != null)
            {
                clusterInfo.NetworkPlugin = aksCluster.NetworkProfile.NetworkPlugin;
                clusterInfo.NetworkPolicy = aksCluster.NetworkProfile.NetworkPolicy;
            }
            
            // Add node pools
            if (aksCluster.AgentPoolProfiles != null)
            {
                clusterInfo.NodePools = aksCluster.AgentPoolProfiles
                    .Select(p => new NodePoolInfo
                    {
                        Name = p.Name,
                        VmSize = p.VmSize,
                        NodeCount = p.Count,
                        OsType = p.OsType
                    })
                    .ToList();
            }
            
            // Set Azure-specific flag
            clusterInfo.IsAksCluster = true;
        }
    }
    
    /// <summary>
    /// Combined cluster information
    /// </summary>
    public class ClusterInfo
    {
        /// <summary>
        /// Unique identifier for the cluster
        /// </summary>
        public string ClusterId { get; set; }
        
        /// <summary>
        /// Full Azure resource ID (if applicable)
        /// </summary>
        public string ResourceId { get; set; }
        
        /// <summary>
        /// Cluster name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Subscription ID (if applicable)
        /// </summary>
        public string SubscriptionId { get; set; }
        
        /// <summary>
        /// Resource group name (if applicable)
        /// </summary>
        public string ResourceGroupName { get; set; }
        
        /// <summary>
        /// Azure region (if applicable)
        /// </summary>
        public string Location { get; set; }
        
        /// <summary>
        /// Kubernetes version
        /// </summary>
        public string KubernetesVersion { get; set; }
        
        /// <summary>
        /// Current status
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// Node resource group name (AKS-specific)
        /// </summary>
        public string NodeResourceGroupName { get; set; }
        
        /// <summary>
        /// Labels/tags
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Network plugin (e.g., "azure", "kubenet")
        /// </summary>
        public string NetworkPlugin { get; set; }
        
        /// <summary>
        /// Network policy (e.g., "calico")
        /// </summary>
        public string NetworkPolicy { get; set; }
        
        /// <summary>
        /// Flag indicating if this is an AKS cluster
        /// </summary>
        public bool IsAksCluster { get; set; }
        
        /// <summary>
        /// Flag indicating if StackRox integration is enabled
        /// </summary>
        public bool StackRoxIntegrated { get; set; }
        
        /// <summary>
        /// Node pools
        /// </summary>
        public List<NodePoolInfo> NodePools { get; set; } = new List<NodePoolInfo>();
        
        /// <summary>
        /// Get a tenant/owner ID for this cluster based on tags
        /// </summary>
        public string GetTenantId()
        {
            // Check standard tag keys that might indicate ownership
            string[] ownershipKeys = { "Owner", "Team", "BusinessUnit", "CostCenter", "Department" };
            
            foreach (var key in ownershipKeys)
            {
                if (Labels.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            
            // Fall back to resource group if available
            return ResourceGroupName;
        }
    }
    
    /// <summary>
    /// Node pool information
    /// </summary>
    public class NodePoolInfo
    {
        /// <summary>
        /// Pool name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// VM size
        /// </summary>
        public string VmSize { get; set; }
        
        /// <summary>
        /// Number of nodes
        /// </summary>
        public int NodeCount { get; set; }
        
        /// <summary>
        /// OS type
        /// </summary>
        public string OsType { get; set; }
    }
}
