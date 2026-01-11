using System;
using System.Collections.Generic;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace emc.camus.observability.otel.Telemetry
{
    public static class MeterProviderBuilderExtensions
    {
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