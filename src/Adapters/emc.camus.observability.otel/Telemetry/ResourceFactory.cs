using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Factory for creating OpenTelemetry resource builders with standard attributes.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ResourceFactory
    {
        /// <summary>
        /// Creates an OpenTelemetry resource builder with service and environment attributes.
        /// </summary>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>A configured OpenTelemetry ResourceBuilder.</returns>
        public static ResourceBuilder Create(
            string serviceName, 
            string serviceVersion, 
            string instanceId, 
            string environmentName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
            ArgumentException.ThrowIfNullOrWhiteSpace(serviceVersion);
            ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
            ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

            return ResourceBuilder.CreateDefault()
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", environmentName),
                    new KeyValuePair<string, object>("service.instance.id", instanceId)
                });
        }
    }
}
