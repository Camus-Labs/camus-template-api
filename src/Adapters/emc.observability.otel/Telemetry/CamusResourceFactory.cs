using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Factory for creating OpenTelemetry resource builders with Camus conventions.
    /// </summary>
    public static class CamusResourceFactory
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
