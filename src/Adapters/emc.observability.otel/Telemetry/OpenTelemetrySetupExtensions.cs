using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Extensions.Hosting;

namespace emc.camus.observability.otel.Telemetry
{
    /// <summary>
    /// Extension methods for configuring OpenTelemetry tracing and metrics for .NET applications using Camus conventions.
    /// </summary>
    public static class OpenTelemetrySetupExtensions
    {
        /// <summary>
        /// Adds and configures OpenTelemetry tracing and metrics to the service collection using Camus conventions.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configuration">Application configuration (expects OpenTelemetry:Tracing and OpenTelemetry:Metrics sections).</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The configured service collection.</returns>
        public static IServiceCollection AddCamusOpenTelemetry(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName,
            string serviceVersion,
            string instanceId,
            string environmentName)
        {
            services
                .AddOpenTelemetry()
                .WithCamusTracing(configuration, serviceName, serviceVersion, instanceId, environmentName)
                .WithCamusMetrics(configuration, serviceName, serviceVersion, instanceId, environmentName);

            return services;
        }
    }
}
