using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Builder;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry metrics and tracing using Camus conventions.
    /// </summary>
    public static class OpenTelemetryBuilderExtensions
    {
        /// <summary>
        /// Configures OpenTelemetry metrics for the application using Camus conventions.
        /// Adds resource attributes, ASP.NET Core, HTTP client, runtime, and process instrumentation, and configures metrics exporter.
        /// </summary>
        /// <param name="builder">The OpenTelemetry builder.</param>
        /// <param name="configuration">Application configuration (expects OpenTelemetry:Metrics section).</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured OpenTelemetry builder.</returns>
        public static OpenTelemetryBuilder WithCamusMetrics(
            this OpenTelemetryBuilder builder,
            IConfiguration configuration,
            string serviceName,
            string serviceVersion,
            string instanceId,
            string environmentName)
        {
            builder.WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder
                    .UseCamusResource(serviceName, serviceVersion, instanceId, environmentName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .ConfigureCamusMetricsExporter(configuration);
            });

            return builder;
        }

        /// <summary>
        /// Configures OpenTelemetry tracing for the application using Camus conventions.
        /// Adds resource attributes, ASP.NET Core and HTTP client instrumentation, and configures tracing exporter.
        /// </summary>
        /// <param name="builder">The OpenTelemetry builder.</param>
        /// <param name="configuration">Application configuration (expects OpenTelemetry:Tracing section).</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured OpenTelemetry builder.</returns>
        public static OpenTelemetryBuilder WithCamusTracing(
            this OpenTelemetryBuilder builder,
            IConfiguration configuration,
            string serviceName,
            string serviceVersion,
            string instanceId,
            string environmentName)
        {
            builder.WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .UseCamusResource(serviceName, serviceVersion, instanceId, environmentName)
                    .AddCamusAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .ConfigureTracingExporter(configuration);
            });

            return builder;
        }
    }
}
