using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry metrics provider.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class MeterProviderBuilderExtensions
    {
        /// <summary>
        /// Sets resource attributes for the metrics provider.
        /// </summary>
        /// <param name="builder">The metrics provider builder.</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured metrics provider builder.</returns>
        public static MeterProviderBuilder UseResourceAttributes(
            this MeterProviderBuilder builder, 
            string serviceName, 
            string serviceVersion,  
            string instanceId,
            string environmentName)
        {
            builder.SetResourceBuilder(ResourceFactory.Create(serviceName, serviceVersion, instanceId, environmentName));
            return builder;
        }

        /// <summary>
        /// Configures the metrics exporter for OpenTelemetry using Camus conventions.
        /// Supported exporters: OTLP (default), Console.
        /// </summary>
        /// <param name="builder">The metrics provider builder.</param>
        /// <param name="settings">OpenTelemetry configuration settings.</param>
        /// <returns>The configured metrics provider builder.</returns>
        public static MeterProviderBuilder ConfigureMetricsExporter(
            this MeterProviderBuilder builder, 
            OpenTelemetrySettings settings)
        {
            var selectedExporter = settings.Metrics.Exporter;

            switch (selectedExporter.ToLowerInvariant())
            {
                case var _ when selectedExporter.Equals(ExporterTypes.Otlp, StringComparison.OrdinalIgnoreCase):
                    builder.AddOtlpExporter(options =>
                    {
                        var endpoint = settings.Metrics.OtlpEndpoint;
                        if (!string.IsNullOrWhiteSpace(endpoint))
                        {
                            options.Endpoint = new Uri(endpoint);
                        }
                        // Use OTLP over gRPC (port 4317)
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                    break;

                case var _ when selectedExporter.Equals(ExporterTypes.Console, StringComparison.OrdinalIgnoreCase):
                    builder.AddConsoleExporter();
                    break;

                default:
                    // No exporter configured
                    break;
            }

            return builder;
        }
    }
}