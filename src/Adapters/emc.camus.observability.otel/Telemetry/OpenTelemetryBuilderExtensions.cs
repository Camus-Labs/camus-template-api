using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Builder;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry metrics and tracing.
    /// </summary>
    public static class OpenTelemetryBuilderExtensions
    {
        /// <summary>
        /// Configures OpenTelemetry metrics for the application.
        /// Adds resource attributes, ASP.NET Core, HTTP client, runtime, and process instrumentation, and configures metrics exporter.
        /// </summary>
        /// <param name="builder">The OpenTelemetry builder.</param>
        /// <param name="settings">OpenTelemetry settings containing metrics exporter configuration.</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured OpenTelemetry builder.</returns>
        public static OpenTelemetryBuilder WithMetricsConfiguration(
            this OpenTelemetryBuilder builder,
            OpenTelemetrySettings settings,
            string serviceName,
            string serviceVersion,
            string instanceId,
            string environmentName)
        {
            builder.WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder
                    .UseResourceAttributes(serviceName, serviceVersion, instanceId, environmentName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .ConfigureMetricsExporter(settings);
            });

            return builder;
        }

        /// <summary>
        /// Configures OpenTelemetry tracing for the application.
        /// Adds resource attributes, ASP.NET Core, HTTP client, custom ActivitySource instrumentation, and configures tracing exporter.
        /// </summary>
        /// <param name="builder">The OpenTelemetry builder.</param>
        /// <param name="settings">OpenTelemetry settings containing tracing exporter configuration.</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured OpenTelemetry builder.</returns>
        public static OpenTelemetryBuilder WithTracingConfiguration(
            this OpenTelemetryBuilder builder,
            OpenTelemetrySettings settings,
            string serviceName,
            string serviceVersion,
            string instanceId,
            string environmentName)
        {
            builder.WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .UseResourceAttributes(serviceName, serviceVersion, instanceId, environmentName)
                    .AddAspNetCoreInstrumentationWithEnrichment()
                    .AddHttpClientInstrumentation()
                    .ConfigureTracingExporter(settings);
            });

            return builder;
        }
    }
}
