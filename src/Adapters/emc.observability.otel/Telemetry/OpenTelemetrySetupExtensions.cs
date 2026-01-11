using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Extensions.Hosting;

namespace emc.camus.observability.otel.Telemetry
{
    public static class OpenTelemetrySetupExtensions
    {
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
