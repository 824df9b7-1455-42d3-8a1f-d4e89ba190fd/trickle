using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Trickle.Common.Configuration;
using Trickle.Common.Dimensions;
using Trickle.Common.Infrastructure;
using Trickle.Common.Messaging;
using Trickle.Common.State;
using Trickle.ContainerSecurity.DataSources.Dimensions;
using Trickle.ContainerSecurity.DataSources.Services;
using Trickle.ContainerSecurity.Processors.CSharp;
using Trickle.ContainerSecurity.DataSources.Events.CSharp;

namespace Trickle.ContainerSecurity
{
    /// <summary>
    /// Startup configuration for Container Security domain
    /// </summary>
    public class Startup : TrickleFunctionStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // Register dimension sources
            services.AddSingleton<CveAllowlistProvider>();
            services.AddSingleton<ClusterProvider>();
            
            // Register dimensions
            services.AddDimensionFramework();
            
            services.AddDimension<CveAllowlistEntry>(builder =>
            {
                var provider = builder.ServiceProvider.GetRequiredService<CveAllowlistProvider>();
                var logger = builder.ServiceProvider.GetRequiredService<ILogger<FileDimension<CveAllowlistEntry>>>();
                
                return new CustomDimension<CveAllowlistEntry>(
                    provider.GetCurrentDataAsync,
                    entry => entry.CveId,
                    logger,
                    TimeSpan.FromHours(1));
            });
            
            services.AddDimension<ClusterInfo>(builder =>
            {
                var provider = builder.ServiceProvider.GetRequiredService<ClusterProvider>();
                var logger = builder.ServiceProvider.GetRequiredService<ILogger<FileDimension<ClusterInfo>>>();
                
                return new CustomDimension<ClusterInfo>(
                    provider.GetCurrentDataAsync,
                    cluster => cluster.ClusterId,
                    logger,
                    TimeSpan.FromMinutes(30));
            });
            
            services.AddDimension<VulnerabilityThreshold>(builder =>
            {
                return new FileDimension<VulnerabilityThreshold>(
                    "config/dimensions/vulnerability-thresholds.json",
                    threshold => threshold.TenantId,
                    builder.ServiceProvider.GetRequiredService<ILogger<FileDimension<VulnerabilityThreshold>>>(),
                    TimeSpan.FromMinutes(15));
            });
            
            // Register state repositories
            services.AddStateManagement(options =>
            {
                options.ConnectionString = Environment.GetEnvironmentVariable("PostgresConnection");
                options.SchemaName = "container_security";
            });
            
            services.AddSingleton<ContainerVulnerabilityRepository>();
            
            // Register StackRox client
            services.Configure<StackRoxClientOptions>(options =>
            {
                options.BaseUrl = Environment.GetEnvironmentVariable("StackRoxBaseUrl");
                options.ApiToken = Environment.GetEnvironmentVariable("StackRoxApiToken");
                options.TimeoutSeconds = 30;
                options.MaxRetries = 3;
            });
            
            services.AddHttpClient<StackRoxClient>((client) =>
            {
                // StackRox client is configured in the constructor
            });
            
            // Register generic HttpClient for notifications
            services.AddHttpClient();
            
            // Register Kubernetes metadata service
            services.AddSingleton(sp => new TokenCredentials(
                Environment.GetEnvironmentVariable("AzureManagementToken")));
                
            services.AddSingleton<KubernetesMetadataService>();
            
            // Register regional context
            services.AddSingleton(sp =>
            {
                var isRegional = bool.TryParse(Environment.GetEnvironmentVariable("IsRegionalInstance"), out var result) && result;
                
                return new RegionalContext
                {
                    IsRegionalInstance = isRegional,
                    Region = Environment.GetEnvironmentVariable("Region"),
                    SupportedSubscriptions = Environment.GetEnvironmentVariable("SupportedSubscriptions")?.Split(',') ?? Array.Empty<string>()
                };
            });
            
            // Register event detector
            services.AddSingleton<StackRoxVulnerabilityDetector>();
        }
    }
}
