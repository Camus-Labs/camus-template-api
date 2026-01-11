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
    public static class OpenTelemetryBuilderExtensions
    {
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
