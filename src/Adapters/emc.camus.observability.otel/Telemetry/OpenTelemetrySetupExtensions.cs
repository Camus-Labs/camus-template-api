using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Extensions.Hosting;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry tracing and metrics for .NET applications.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class OpenTelemetrySetupExtensions
    {
        /// <summary>
        /// Adds and configures OpenTelemetry tracing and metrics to the service collection.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="settings">OpenTelemetry settings containing tracing, metrics, and logs exporter configuration.</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured service collection.</returns>
        public static IServiceCollection AddOpenTelemetryServices(
            this IServiceCollection services,
            OpenTelemetrySettings settings,
            string serviceName,
            string serviceVersion,
            string instanceId,
            string environmentName)
        {
            services
                .AddOpenTelemetry()
                .WithTracingConfiguration(settings, serviceName, serviceVersion, instanceId, environmentName)
                .WithMetricsConfiguration(settings, serviceName, serviceVersion, instanceId, environmentName);

            return services;
        }
    }
}
