using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using emc.camus.observability.otel.Logging;
using emc.camus.observability.otel.Telemetry;

namespace emc.camus.observability.otel
{
    public static class ObservabilitySetupExtensions
    {
        private const string DefaultServiceName = "unknown-service-name";
        private const string DefaultServiceVersion = "unknown-service-version";
        private const string DefaultEnvironmentName = "unknown-environment";
        private const string DefaultInstanceId = "unknown-instance-id";

        public static WebApplicationBuilder ConfigureCamusObservability(
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

            builder.Host.UseEmcSerilog(configuration, normalizedServiceName, normalizedServiceVersion, normalizedInstanceId, normalizedEnvironmentName);
            builder.Services.AddCamusOpenTelemetry(configuration, normalizedServiceName, normalizedServiceVersion, normalizedInstanceId, normalizedEnvironmentName);
            // Ensure Serilog flushes on shutdown to avoid log loss
            builder.Services.AddHostedService<SerilogFlushHostedService>();
            return builder;
        }
    }
}
