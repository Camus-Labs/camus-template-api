using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using OpenTelemetry.Exporter;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace emc.camus.observability.otel.Logging
{
    /// <summary>
    /// Extension methods for <see cref="LoggerConfiguration"/> to apply common enrichers and sinks.
    /// </summary>
    public static class LoggerConfigurationExtensions
    {
        /// <summary>
        /// Applies default enrichers (LogContext and ActivityCurrentEnricher) to the logger configuration.
        /// </summary>
        public static LoggerConfiguration ApplyDefaultEnrichers(this LoggerConfiguration loggerConfiguration)
        {
            return loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.With(new ActivityCurrentEnricher());
        }

        /// <summary>
        /// Applies the Serilog Console sink based on <c>Logging:Console</c> configuration only.
        /// Defaults to enabled with a template including trace/span ids.
        /// </summary>
        public static LoggerConfiguration WriteConsoleLogging(
            this LoggerConfiguration loggerConfiguration,
            IConfiguration configuration)
        {
            var loggingConsole = configuration.GetSection("Logging:Console");

            // Single-return pattern
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

        /// <summary>
        /// Configures Serilog to export logs via OTLP to an OpenTelemetry Collector when enabled in configuration.
        /// Reads <c>OpenTelemetry:Logs:Exporter</c> and <c>OpenTelemetry:Logs:OtlpEndpoint</c>.
        /// Includes trace/span fields in the OTLP record so Collector debug shows them at the top.
        /// </summary>
        public static LoggerConfiguration WriteLogsToOpenTelemetry(
            this LoggerConfiguration loggerConfiguration,
            IConfiguration configuration,
            IHostEnvironment environment,
            string serviceName,
            string serviceVersion)
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
                        ["deployment.environment"] = environment.EnvironmentName ?? "unknown"
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
