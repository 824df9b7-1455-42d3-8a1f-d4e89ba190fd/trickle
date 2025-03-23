using System;
using Azure.Messaging.ServiceBus;
using Kusto.Data;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Trickle.Common.Messaging
{
    /// <summary>
    /// Extension methods for registering messaging services
    /// </summary>
    public static class MessagingServiceExtensions
    {
        /// <summary>
        /// Add event messaging services
        /// </summary>
        public static IServiceCollection AddEventMessaging(
            this IServiceCollection services,
            Action<EventMessagingOptions> configureOptions)
        {
            // Configure options
            services.Configure(configureOptions);
            
            // Add Service Bus client
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<EventMessagingOptions>>().Value;
                return new ServiceBusClient(options.Value.ServiceBusConnectionString);
            });
            
            // Add Kusto clients
            services.AddSingleton<IKustoQueuedIngestClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<EventMessagingOptions>>().Value;
                
                // Create connection string builder
                var kcsb = new KustoConnectionStringBuilder(options.KustoIngestUri)
                {
                    FederatedSecurity = true,
                    ApplicationToken = options.KustoConnectionString
                };
                
                return KustoIngestFactory.CreateQueuedIngestClient(kcsb);
            });
            
            services.AddSingleton<ICslAdminProvider>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<EventMessagingOptions>>().Value;
                
                // Create connection string builder
                var kcsb = new KustoConnectionStringBuilder(options.KustoDataSourceUri)
                {
                    FederatedSecurity = true,
                    ApplicationToken = options.KustoConnectionString
                };
                
                return KustoClientFactory.CreateCslAdminProvider(kcsb);
            });
            
            // Add security event publisher
            services.AddSingleton<ISecurityEventPublisher, SecurityEventPublisher>();
            
            return services;
        }
    }
}
