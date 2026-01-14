using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System;
using OpenTelemetry.Exporter;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace emc.camus.observability.otel.Logging
{
    /// <summary>
    /// Extension methods for configuring Serilog loggers with Camus observability conventions.
    /// Includes enrichers, console logging, and OpenTelemetry log export.
    /// </summary>
    public static class LoggerConfigurationExtensions
    {
        /// <summary>
        /// Adds default enrichers to the Serilog logger configuration, including log context and activity enrichment.
        /// </summary>
        /// <param name="loggerConfiguration">The Serilog logger configuration.</param>
        /// <returns>The updated logger configuration.</returns>
        public static LoggerConfiguration ApplyDefaultEnrichers(
            this LoggerConfiguration loggerConfiguration)
        {
            return loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.With(new ActivityCurrentEnricher());
        }
        
        /// <summary>
        /// Configures Serilog to write logs to the console. Intended for development and debugging.
        /// </summary>
        /// <param name="loggerConfiguration">The Serilog logger configuration.</param>
        /// <param name="configuration">Application configuration (expects Logging:Console section).</param>
        /// <returns>The updated logger configuration.</returns>
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

        /// <summary>
        /// Configures Serilog to export logs to OpenTelemetry using the OTLP protocol.
        /// The collector can route logs to Loki or other supported backends.
        /// </summary>
        /// <param name="loggerConfiguration">The Serilog logger configuration.</param>
        /// <param name="configuration">Application configuration (expects OpenTelemetry:Logs section).</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The updated logger configuration.</returns>
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
