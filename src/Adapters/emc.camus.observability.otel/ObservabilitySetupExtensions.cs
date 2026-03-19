using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using emc.camus.observability.otel.Configurations;
using emc.camus.observability.otel.Logging;
using emc.camus.observability.otel.Telemetry;
using emc.camus.observability.otel.Services;
using emc.camus.observability.otel.Middleware;
using emc.camus.application.Observability;

namespace emc.camus.observability.otel
{
    /// <summary>
    /// Extension methods for configuring observability (logging, tracing, metrics) in a .NET application.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ObservabilitySetupExtensions
    {
        private const string DefaultServiceName = "unknown-service-name";
        private const string DefaultServiceVersion = "unknown-service-version";
        private const string DefaultEnvironmentName = "unknown-environment";
        private const string DefaultInstanceId = "unknown-instance-id";

        /// <summary>
        /// Configures observability for the application, including logging (Serilog), tracing, and metrics (OpenTelemetry).
        /// Sets up resource attributes, exporters, and ensures logs are flushed on shutdown.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to configure.</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured WebApplicationBuilder.</returns>
        public static WebApplicationBuilder AddObservability(
            this WebApplicationBuilder builder,
            string serviceName,
            string serviceVersion,
            string instanceId,
            string environmentName)
        {
            var configuration = builder.Configuration;
            var normalizedServiceName = string.IsNullOrWhiteSpace(serviceName) ? DefaultServiceName : serviceName.Trim();
            var normalizedServiceVersion = string.IsNullOrWhiteSpace(serviceVersion) ? DefaultServiceVersion : serviceVersion.Trim();
            var normalizedEnvironmentName = string.IsNullOrWhiteSpace(environmentName) ? DefaultEnvironmentName : environmentName.Trim();
            var normalizedInstanceId = string.IsNullOrWhiteSpace(instanceId) ? DefaultInstanceId : instanceId.Trim();

            // Parse OpenTelemetry settings once from configuration
            var settings = configuration.GetSection(OpenTelemetrySettings.ConfigurationSectionName).Get<OpenTelemetrySettings>() ?? new OpenTelemetrySettings();
            settings.Validate();
            builder.Services.AddSingleton(settings);

            builder.Host.UseSerilogWithOpenTelemetry(settings, normalizedServiceName, normalizedServiceVersion, normalizedInstanceId, normalizedEnvironmentName);
            builder.Services.AddOpenTelemetryServices(settings, normalizedServiceName, normalizedServiceVersion, normalizedInstanceId, normalizedEnvironmentName);
            
            // Register ActivitySource and wrapper for distributed tracing
            builder.Services.AddSingleton(_ => new ActivitySource(normalizedServiceName, normalizedServiceVersion));
            builder.Services.AddSingleton<IActivitySourceWrapper, ActivitySourceWrapper>();
            
            // Ensure Serilog flushes on shutdown to avoid log loss
            builder.Services.AddHostedService<SerilogFlushHostedService>();

            return builder;
        }

        /// <summary>
        /// Adds observability middleware to the application pipeline.
        /// Must be called early in the pipeline, before exception handling middleware,
        /// so that trace IDs are available in error logs.
        /// </summary>
        /// <param name="app">The WebApplication instance.</param>
        /// <returns>The WebApplication instance for method chaining.</returns>
        public static WebApplication UseObservability(this WebApplication app)
        {
            app.UseMiddleware<TraceIdHeaderMiddleware>();

            return app;
        }
    }
}
