using System;
using System.Collections.Generic;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry metrics provider using Camus conventions.
    /// </summary>
    public static class MeterProviderBuilderExtensions
    {
        /// <summary>
        /// Sets resource attributes for the metrics provider using Camus conventions.
        /// </summary>
        /// <param name="builder">The metrics provider builder.</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured metrics provider builder.</returns>
        public static MeterProviderBuilder UseCamusResource(
            this MeterProviderBuilder builder, 
            string serviceName, 
            string serviceVersion,  
            string instanceId,
            string environmentName)
        {
            builder.SetResourceBuilder(CamusResourceFactory.Create(serviceName, serviceVersion, instanceId, environmentName));
            return builder;
        }

        /// <summary>
        /// Configures the metrics exporter for OpenTelemetry using Camus conventions.
        /// Supported exporters: OTLP (default), Console.
        /// </summary>
        /// <param name="builder">The metrics provider builder.</param>
        /// <param name="configuration">Application configuration (expects OpenTelemetry:Metrics section).</param>
        /// <returns>The configured metrics provider builder.</returns>
        public static MeterProviderBuilder ConfigureCamusMetricsExporter(
            this MeterProviderBuilder builder, 
            IConfiguration configuration)
        {
            var openTelemetryConfig = configuration.GetSection("OpenTelemetry");
            var selectedExporter = openTelemetryConfig["Metrics:Exporter"] ?? "none";

            switch (selectedExporter.ToLowerInvariant())
            {
                case "otlp":
                    builder.AddOtlpExporter(options =>
                    {
                        var endpoint = openTelemetryConfig["Metrics:OtlpEndpoint"];
                        if (!string.IsNullOrWhiteSpace(endpoint))
                        {
                            options.Endpoint = new Uri(endpoint);
                        }
                        // Use OTLP over gRPC (port 4317)
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                    break;

                case "console":
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