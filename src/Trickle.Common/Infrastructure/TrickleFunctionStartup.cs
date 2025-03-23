using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Trickle.Common.Infrastructure
{
    /// <summary>
    /// Base class for Trickle function startup
    /// </summary>
    public abstract class TrickleFunctionStartup
    {
        /// <summary>
        /// Configure services for this function app
        /// </summary>
        public virtual void ConfigureServices(IServiceCollection services)
        {
            // Override in derived classes to add domain-specific services
        }
        
        /// <summary>
        /// Create the host for this function app
        /// </summary>
        public IHost CreateHost()
        {
            return BuildHost((context, services) =>
            {
                ConfigureServices(services);
            });
        }
        
        /// <summary>
        /// Build the host with core services
        /// </summary>
        protected virtual IHost BuildHost(Action<HostBuilderContext, IServiceCollection> configureServices = null)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    // Configure function middleware
                    worker.UseMiddleware<CorrelationMiddleware>();
                    worker.UseMiddleware<TenantIsolationMiddleware>();
                    worker.UseMiddleware<ExceptionHandlingMiddleware>();
                })
                .ConfigureAppConfiguration((context, builder) =>
                {
                    // Build standard configuration
                    builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                           .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                           .AddEnvironmentVariables();
                           
                    // Add Key Vault configuration if available
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_KEYVAULT_ENDPOINT")))
                    {
                        builder.AddAzureKeyVault(
                            new Uri(Environment.GetEnvironmentVariable("AZURE_KEYVAULT_ENDPOINT")),
                            new Azure.Identity.DefaultAzureCredential());
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    // Configure core services
                    ConfigureTrickleServices(context.Configuration, services);
                    
                    // Call additional configuration if provided
                    configureServices?.Invoke(context, services);
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddConsole();
                    builder.AddApplicationInsights(
                        options => options.ConnectionString = context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"],
                        _ => { });
                });
                
            return host.Build();
        }
        
        /// <summary>
        /// Configure core Trickle services
        /// </summary>
        protected virtual void ConfigureTrickleServices(
            IConfiguration configuration,
            IServiceCollection services)
        {
            // Configure telemetry
            services.AddSingleton<ITelemetryInitializer, TrickleTelemetryInitializer>();
            
            // Register regional context
            services.AddSingleton(sp =>
            {
                var isRegional = bool.TryParse(Environment.GetEnvironmentVariable("IsRegionalInstance"), out var result) && result;
                
                return new RegionalContext
                {
                    IsRegionalInstance = isRegional,
                    Region = Environment.GetEnvironmentVariable("Region"),
                    SupportedSubscriptions = Environment.GetEnvironmentVariable("SupportedSubscriptions")?.Split(',') ?? Array.Empty<string>(),
                    IsGlobalCoordinator = bool.TryParse(Environment.GetEnvironmentVariable("IsGlobalCoordinator"), out var coordinator) && coordinator
                };
            });
            
            // Add Configuration services
            services.AddTrickleConfiguration(options =>
            {
                configuration.GetSection("Trickle").Bind(options);
            });
            
            // Add dimension framework
            services.AddDimensionFramework();
            
            // Add event messaging
            services.AddEventMessaging(options =>
            {
                options.ServiceBusConnectionString = configuration.GetConnectionString("ServiceBus");
                options.KustoConnectionString = configuration.GetConnectionString("KustoIngestion");
                options.KustoIngestUri = configuration["Kusto:IngestUri"];
                options.KustoDataSourceUri = configuration["Kusto:DataSourceUri"];
            });
        }
    }
    
    /// <summary>
    /// Telemetry initializer to add custom properties
    /// </summary>
    public class TrickleTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHostEnvironment _environment;
        
        public TrickleTelemetryInitializer(IHostEnvironment environment)
        {
            _environment = environment;
        }
        
        public void Initialize(Microsoft.ApplicationInsights.Channel.ITelemetry telemetry)
        {
            // Add environment name
            telemetry.Context.Cloud.RoleName = "Trickle Platform";
            telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
            
            // Add environment tag
            if (!telemetry.Context.Properties.ContainsKey("Environment"))
            {
                telemetry.Context.Properties["Environment"] = _environment.EnvironmentName;
            }
            
            // Add tenant ID from current activity if available
            var activity = System.Diagnostics.Activity.Current;
            if (activity != null)
            {
                var tenantId = activity.Tags.GetValueOrDefault("trickle.tenant_id");
                if (!string.IsNullOrEmpty(tenantId) && !telemetry.Context.Properties.ContainsKey("TenantId"))
                {
                    telemetry.Context.Properties["TenantId"] = tenantId;
                }
                
                var correlationId = activity.Tags.GetValueOrDefault("trickle.correlation_id");
                if (!string.IsNullOrEmpty(correlationId) && !telemetry.Context.Properties.ContainsKey("CorrelationId"))
                {
                    telemetry.Context.Properties["CorrelationId"] = correlationId;
                }
            }
        }
    }
}
