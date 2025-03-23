using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Trickle.ContainerSecurity.Domain.Models;

namespace Trickle.ContainerSecurity.DataSources.Services
{
    /// <summary>
    /// Configuration options for StackRox client
    /// </summary>
    public class StackRoxClientOptions
    {
        /// <summary>
        /// Base URL for StackRox API
        /// </summary>
        public string BaseUrl { get; set; }
        
        /// <summary>
        /// API token for authentication
        /// </summary>
        public string ApiToken { get; set; }
        
        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Maximum retries for transient failures
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }
    
    /// <summary>
    /// Client for interacting with StackRox API
    /// </summary>
    public class StackRoxClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StackRoxClient> _logger;
        private readonly StackRoxClientOptions _options;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        
        public StackRoxClient(
            HttpClient httpClient,
            IOptions<StackRoxClientOptions> options,
            ILogger<StackRoxClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
            
            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            
            // Create retry policy
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => 
                    (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                .WaitAndRetryAsync(
                    _options.MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (outcome, timespan, retryCount, context) =>
                    {
                        if (outcome.Exception != null)
                        {
                            _logger.LogWarning(
                                outcome.Exception, 
                                "Error calling StackRox API. Retrying {RetryCount}/{MaxRetries} after {RetryDelay}ms",
                                retryCount,
                                _options.MaxRetries,
                                timespan.TotalMilliseconds);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "StackRox API returned {StatusCode}. Retrying {RetryCount}/{MaxRetries} after {RetryDelay}ms",
                                outcome.Result.StatusCode,
                                retryCount,
                                _options.MaxRetries,
                                timespan.TotalMilliseconds);
                        }
                    });
        }
        
        /// <summary>
        /// Get vulnerabilities from StackRox
        /// </summary>
        public async Task<List<Vulnerability>> GetVulnerabilitiesAsync(
            string clusterId = null,
            string severity = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string requestUri = "v1/vulnerabilities";
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(clusterId))
                    queryParams.Add($"cluster_id={Uri.EscapeDataString(clusterId)}");
                    
                if (!string.IsNullOrEmpty(severity))
                    queryParams.Add($"severity={Uri.EscapeDataString(severity)}");
                    
                if (queryParams.Count > 0)
                    requestUri += "?" + string.Join("&", queryParams);
                
                _logger.LogDebug("Calling StackRox API: GET {RequestUri}", requestUri);
                
                var response = await _retryPolicy.ExecuteAsync(async () => 
                    await _httpClient.GetAsync(requestUri, cancellationToken));
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<VulnerabilityResponse>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return result?.Vulnerabilities ?? new List<Vulnerability>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vulnerabilities from StackRox");
                throw;
            }
        }
        
        /// <summary>
        /// Get containers with vulnerabilities
        /// </summary>
        public async Task<List<Container>> GetContainersWithVulnerabilitiesAsync(
            string clusterId = null,
            string namespace_ = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string requestUri = "v1/containers";
                var queryParams = new List<string> { "with_vulns=true" };
                
                if (!string.IsNullOrEmpty(clusterId))
                    queryParams.Add($"cluster_id={Uri.EscapeDataString(clusterId)}");
                    
                if (!string.IsNullOrEmpty(namespace_))
                    queryParams.Add($"namespace={Uri.EscapeDataString(namespace_)}");
                    
                requestUri += "?" + string.Join("&", queryParams);
                
                _logger.LogDebug("Calling StackRox API: GET {RequestUri}", requestUri);
                
                var response = await _retryPolicy.ExecuteAsync(async () => 
                    await _httpClient.GetAsync(requestUri, cancellationToken));
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ContainerResponse>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return result?.Containers ?? new List<Container>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting containers with vulnerabilities from StackRox");
                throw;
            }
        }
        
        /// <summary>
        /// Get clusters from StackRox
        /// </summary>
        public async Task<List<KubernetesCluster>> GetClustersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                string requestUri = "v1/clusters";
                
                _logger.LogDebug("Calling StackRox API: GET {RequestUri}", requestUri);
                
                var response = await _retryPolicy.ExecuteAsync(async () => 
                    await _httpClient.GetAsync(requestUri, cancellationToken));
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ClusterResponse>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                return result?.Clusters ?? new List<KubernetesCluster>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clusters from StackRox");
                throw;
            }
        }
        
        // Helper classes for deserialization
        
        private class VulnerabilityResponse
        {
            public List<Vulnerability> Vulnerabilities { get; set; }
        }
        
        private class ContainerResponse
        {
            public List<Container> Containers { get; set; }
        }
        
        private class ClusterResponse
        {
            public List<KubernetesCluster> Clusters { get; set; }
        }
    }
    
    /// <summary>
    /// Kubernetes cluster information
    /// </summary>
    public class KubernetesCluster
    {
        /// <summary>
        /// Cluster ID
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Cluster name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Status (e.g., "ACTIVE", "UNAVAILABLE")
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// Kubernetes version
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Labels
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    }
}
