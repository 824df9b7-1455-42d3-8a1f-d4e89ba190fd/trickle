using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trickle.Common.Domain;

namespace Trickle.Common.Messaging
{
    /// <summary>
    /// Interface for publishing security events
    /// </summary>
    public interface ISecurityEventPublisher
    {
        /// <summary>
        /// Publish a security event
        /// </summary>
        /// <typeparam name="T">Type of security event</typeparam>
        /// <param name="securityEvent">The security event to publish</param>
        /// <param name="customProperties">Optional custom properties for the message</param>
        /// <param name="topicName">Name of the Service Bus topic (default: "security-events")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PublishEventAsync<T>(
            T securityEvent, 
            Dictionary<string, object> customProperties = null,
            string topicName = "security-events",
            CancellationToken cancellationToken = default)
            where T : SecurityEvent;
            
        /// <summary>
        /// Store a security event in ADX without publishing to Service Bus
        /// </summary>
        /// <typeparam name="T">Type of security event</typeparam>
        /// <param name="securityEvent">The security event to store</param>
        /// <param name="databaseName">Optional database name override</param>
        /// <param name="tableName">Optional table name override</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StoreEventAsync<T>(
            T securityEvent,
            string databaseName = null,
            string tableName = null,
            CancellationToken cancellationToken = default)
            where T : SecurityEvent;
    }
}
