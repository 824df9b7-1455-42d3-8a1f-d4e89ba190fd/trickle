using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Trickle.Common.Domain;

namespace Trickle.Common.Messaging
{
    /// <summary>
    /// Configuration options for event messaging
    /// </summary>
    public class EventMessagingOptions
    {
        /// <summary>
        /// Connection string for Azure Service Bus
        /// </summary>
        public string ServiceBusConnectionString { get; set; }
        
        /// <summary>
        /// Connection string for Azure Data Explorer
        /// </summary>
        public string KustoConnectionString { get; set; }
        
        /// <summary>
        /// Kusto ingestion URI
        /// </summary>
        public string KustoIngestUri { get; set; }
        
        /// <summary>
        /// Kusto data source URI
        /// </summary>
        public string KustoDataSourceUri { get; set; }
        
        /// <summary>
        /// Default Kusto database name template
        /// </summary>
        public string DefaultDatabaseNameTemplate { get; set; } = "trickle_{0}";
        
        /// <summary>
        /// Default Kusto table name for security events
        /// </summary>
        public string DefaultTableName { get; set; } = "SecurityEvents";
    }
    
    /// <summary>
    /// Implementation of the security event publisher
    /// </summary>
    public class SecurityEventPublisher : ISecurityEventPublisher
    {
        private readonly ILogger<SecurityEventPublisher> _logger;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IKustoQueuedIngestClient _kustoIngestClient;
        private readonly ICslAdminProvider _kustoAdminProvider;
        private readonly EventMessagingOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly AsyncRetryPolicy _serviceBusRetryPolicy;
        private readonly AsyncRetryPolicy _kustoRetryPolicy;
        
        public SecurityEventPublisher(
            ILogger<SecurityEventPublisher> logger,
            ServiceBusClient serviceBusClient,
            IKustoQueuedIngestClient kustoIngestClient,
            ICslAdminProvider kustoAdminProvider,
            IOptions<EventMessagingOptions> options)
        {
            _logger = logger;
            _serviceBusClient = serviceBusClient;
            _kustoIngestClient = kustoIngestClient;
            _kustoAdminProvider = kustoAdminProvider;
            _options = options.Value;
            
            // Initialize JSON serializer options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
            
            // Create Service Bus retry policy
            _serviceBusRetryPolicy = Policy
                .Handle<ServiceBusException>(ex => ex.Reason == ServiceBusFailureReason.ServiceBusy 
                    || ex.Reason == ServiceBusFailureReason.ServiceTimeout
                    || ex.Reason == ServiceBusFailureReason.MessageLockLost)
                .Or<ServiceBusException>(ex => ex.IsTransient)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Error publishing event to Service Bus. Retry {RetryCount} after {RetryDelay}ms",
                            retryCount,
                            timeSpan.TotalMilliseconds);
                    });
                    
            // Create Kusto retry policy
            _kustoRetryPolicy = Policy
                .Handle<Kusto.Data.Exceptions.KustoServiceException>()
                .Or<Kusto.Ingest.Exceptions.KustoIngestionException>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Error storing event in ADX. Retry {RetryCount} after {RetryDelay}ms",
                            retryCount,
                            timeSpan.TotalMilliseconds);
                    });
        }
        
        /// <summary>
        /// Publish a security event
        /// </summary>
        public async Task PublishEventAsync<T>(
            T securityEvent, 
            Dictionary<string, object> customProperties = null,
            string topicName = "security-events",
            CancellationToken cancellationToken = default)
            where T : SecurityEvent
        {
            if (securityEvent == null)
                throw new ArgumentNullException(nameof(securityEvent));
                
            // Validate the event
            if (!securityEvent.Validate(out var validationErrors))
            {
                throw new InvalidOperationException(
                    $"Security event validation failed: {string.Join(", ", validationErrors)}");
            }
            
            try
            {
                // Publish to Service Bus
                await PublishToServiceBusAsync(
                    securityEvent, 
                    customProperties, 
                    topicName, 
                    cancellationToken);
                
                // Store in ADX if this is a retention-eligible event type
                // Some specialized events like notifications might not need to be retained
                if (IsRetentionEligible(securityEvent.EventType))
                {
                    await StoreEventAsync(securityEvent, null, null, cancellationToken);
                }
                
                _logger.LogInformation(
                    "Published {EventType} event {EventId} for owner {OwnerId}",
                    securityEvent.EventType,
                    securityEvent.EventId,
                    securityEvent.OwnerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error publishing {EventType} event {EventId} for owner {OwnerId}",
                    securityEvent.EventType,
                    securityEvent.EventId,
                    securityEvent.OwnerId);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Store a security event in ADX without publishing to Service Bus
        /// </summary>
        public async Task StoreEventAsync<T>(
            T securityEvent,
            string databaseName = null,
            string tableName = null,
            CancellationToken cancellationToken = default)
            where T : SecurityEvent
        {
            if (securityEvent == null)
                throw new ArgumentNullException(nameof(securityEvent));
                
            // Use specified database name or format using the template
            var targetDb = databaseName ?? string.Format(
                _options.DefaultDatabaseNameTemplate, 
                securityEvent.OwnerId?.ToLowerInvariant() ?? "unknown");
                
            // Use specified table name or default
            var targetTable = tableName ?? _options.DefaultTableName;
            
            try
            {
                await _kustoRetryPolicy.ExecuteAsync(async () =>
                {
                    // Ensure the database and table exist
                    await EnsureDatabaseAndTableExistAsync(targetDb, targetTable, cancellationToken);
                    
                    // Get ADX properties
                    var properties = securityEvent.GetAdxProperties();
                    
                    // Create ingestion properties
                    var kustoIngestionProperties = new KustoQueuedIngestionProperties(targetDb, targetTable)
                    {
                        Format = DataSourceFormat.json,
                        IngestionMapping = new IngestionMapping
                        {
                            IngestionMappingReference = $"{targetTable}_mapping",
                            IngestionMappingKind = IngestionMappingKind.Json
                        }
                    };
                    
                    // Serialize to JSON for ingestion
                    var jsonString = securityEvent.ToJson();
                    var dataStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonString));
                    
                    // Ingest the data
                    await _kustoIngestClient.IngestFromStreamAsync(
                        dataStream,
                        kustoIngestionProperties,
                        cancellationToken);
                        
                    _logger.LogDebug(
                        "Stored {EventType} event {EventId} in ADX database {Database}, table {Table}",
                        securityEvent.EventType,
                        securityEvent.EventId,
                        targetDb,
                        targetTable);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error storing {EventType} event {EventId} in ADX database {Database}, table {Table}",
                    securityEvent.EventType,
                    securityEvent.EventId,
                    targetDb,
                    targetTable);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Publish event to Service Bus
        /// </summary>
        private async Task PublishToServiceBusAsync<T>(
            T securityEvent,
            Dictionary<string, object> customProperties,
            string topicName,
            CancellationToken cancellationToken)
            where T : SecurityEvent
        {
            await _serviceBusRetryPolicy.ExecuteAsync(async () =>
            {
                // Create sender
                await using var sender = _serviceBusClient.CreateSender(topicName);
                
                // Create message
                var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(securityEvent.ToJson()))
                {
                    MessageId = securityEvent.EventId,
                    CorrelationId = securityEvent.CorrelationId ?? securityEvent.EventId,
                    ContentType = "application/json",
                    Subject = securityEvent.EventType
                };
                
                // Add standard properties
                message.ApplicationProperties.Add("EventType", securityEvent.EventType);
                message.ApplicationProperties.Add("OwnerId", securityEvent.OwnerId ?? "unknown");
                message.ApplicationProperties.Add("Severity", securityEvent.Severity.ToString());
                message.ApplicationProperties.Add("ResourceId", securityEvent.ResourceId);
                message.ApplicationProperties.Add("DetectedAt", securityEvent.DetectedAt.ToString("o"));
                
                // Add distributed tracing info
                var activity = Activity.Current;
                if (activity != null)
                {
                    message.ApplicationProperties.Add("TraceId", activity.TraceId.ToString());
                    message.ApplicationProperties.Add("SpanId", activity.SpanId.ToString());
                }
                
                // Add custom properties
                if (customProperties != null)
                {
                    foreach (var prop in customProperties)
                    {
                        message.ApplicationProperties.Add(prop.Key, prop.Value);
                    }
                }
                
                // Send message
                await sender.SendMessageAsync(message, cancellationToken);
                
                _logger.LogDebug(
                    "Published {EventType} event {EventId} to topic {TopicName}",
                    securityEvent.EventType,
                    securityEvent.EventId,
                    topicName);
            });
        }
        
        /// <summary>
        /// Ensure the ADX database and table exist
        /// </summary>
        private async Task EnsureDatabaseAndTableExistAsync(
            string databaseName,
            string tableName,
            CancellationToken cancellationToken)
        {
            try
            {
                // Check if database exists, create if not
                var databasesCommand = ".show databases | where DatabaseName == '" + databaseName + "'";
                var databasesResult = await _kustoAdminProvider.ExecuteControlCommandAsync(
                    "", databasesCommand, ClientRequestProperties.Empty);
                    
                bool dbExists = databasesResult.Count > 0;
                
                if (!dbExists)
                {
                    _logger.LogInformation("Creating ADX database {Database}", databaseName);
                    
                    var createDbCommand = $".create database [{databaseName}]";
                    await _kustoAdminProvider.ExecuteControlCommandAsync(
                        "", createDbCommand, ClientRequestProperties.Empty);
                }
                
                // Check if table exists, create if not
                var tablesCommand = $".show tables | where TableName == '{tableName}'";
                var tablesResult = await _kustoAdminProvider.ExecuteControlCommandAsync(
                    databaseName, tablesCommand, ClientRequestProperties.Empty);
                    
                bool tableExists = tablesResult.Count > 0;
                
                if (!tableExists)
                {
                    _logger.LogInformation(
                        "Creating ADX table {Table} in database {Database}", 
                        tableName, databaseName);
                    
                    // Create table
                    var createTableCommand = $@"
                        .create table [{tableName}] (
                            EventId: string,
                            EventType: string,
                            DetectedAt: datetime,
                            OwnerId: string,
                            Severity: string,
                            ResourceId: string,
                            CorrelationId: string,
                            RawEventData: dynamic
                        )";
                        
                    await _kustoAdminProvider.ExecuteControlCommandAsync(
                        databaseName, createTableCommand, ClientRequestProperties.Empty);
                        
                    // Create mapping
                    var createMappingCommand = $@"
                        .create table [{tableName}] ingestion json mapping '{tableName}_mapping'
                        '[
                            {{""column"":""EventId"", ""path"":""$.eventId"", ""datatype"":""string""}},
                            {{""column"":""EventType"", ""path"":""$.eventType"", ""datatype"":""string""}},
                            {{""column"":""DetectedAt"", ""path"":""$.detectedAt"", ""datatype"":""datetime""}},
                            {{""column"":""OwnerId"", ""path"":""$.ownerId"", ""datatype"":""string""}},
                            {{""column"":""Severity"", ""path"":""$.severity"", ""datatype"":""string""}},
                            {{""column"":""ResourceId"", ""path"":""$.resourceId"", ""datatype"":""string""}},
                            {{""column"":""CorrelationId"", ""path"":""$.correlationId"", ""datatype"":""string""}},
                            {{""column"":""RawEventData"", ""path"":""$"", ""datatype"":""dynamic""}}
                        ]'";
                        
                    await _kustoAdminProvider.ExecuteControlCommandAsync(
                        databaseName, createMappingCommand, ClientRequestProperties.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Error ensuring ADX database {Database} and table {Table} exist", 
                    databaseName, tableName);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Determine if an event type should be stored in ADX
        /// </summary>
        private bool IsRetentionEligible(string eventType)
        {
            // By default, store all events
            // You could exclude certain event types like notifications
            // that don't need long-term storage
            
            return !eventType.EndsWith("NotificationEvent", StringComparison.OrdinalIgnoreCase);
        }
    }
}
