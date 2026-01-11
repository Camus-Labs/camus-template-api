using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System;
using OpenTelemetry.Exporter;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace emc.camus.observability.otel.Logging
{
    public static class LoggerConfigurationExtensions
    {
        public static LoggerConfiguration ApplyDefaultEnrichers(
            this LoggerConfiguration loggerConfiguration)
        {
            return loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.With(new ActivityCurrentEnricher());
        }
        
        public static LoggerConfiguration WriteConsoleLogging(
            this LoggerConfiguration loggerConfiguration,
            IConfiguration configuration)
        {
            var loggingConsole = configuration.GetSection("Logging:Console");

            var enabled = loggingConsole.GetValue<bool>("Enabled", true);
            var template = loggingConsole.GetValue<string>(
                "OutputTemplate",
                "[{Timestamp:HH:mm:ss} {Level:u3}] (trace_id={trace_id} span_id={span_id}) {Message:lj}{NewLine}{Exception}");

            var configured = loggerConfiguration;
            if (enabled)
            {
                configured = configured.WriteTo.Console(outputTemplate: template);
            }

            return configured;
        }

        public static LoggerConfiguration WriteLogsToOpenTelemetry(
            this LoggerConfiguration loggerConfiguration,
            IConfiguration configuration,
            string serviceName,
            string serviceVersion,
            string instanceId,
            string environmentName)
        {
            var logsSection = configuration.GetSection("OpenTelemetry:Logs");
            var exporter = logsSection.GetValue<string>("Exporter")?.ToLowerInvariant();

            var configured = loggerConfiguration;
            if (exporter == "otlp")
            {
                var endpoint = logsSection.GetValue<string>("OtlpEndpoint") ?? "http://localhost:4317";
                configured = configured.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = endpoint;
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName,
                        ["service.version"] = serviceVersion,
                        ["deployment.environment"] = environmentName,
                        ["service.instance.id"] = instanceId
                    };
                    options.IncludedData = IncludedData.MessageTemplateTextAttribute
                                         | IncludedData.SpecRequiredResourceAttributes
                                         | IncludedData.TraceIdField
                                         | IncludedData.SpanIdField;
                });
            }

            return configured;
        }
    }
}
