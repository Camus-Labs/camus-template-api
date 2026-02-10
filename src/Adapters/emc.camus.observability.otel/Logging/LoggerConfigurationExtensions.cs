using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System;
using OpenTelemetry.Exporter;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.Logging
{
    /// <summary>
    /// Extension methods for configuring Serilog loggers with Camus observability conventions.
    /// Includes enrichers, console logging, and OpenTelemetry log export.
    /// </summary>
    [ExcludeFromCodeCoverage]
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
        /// <param name="settings">OpenTelemetry settings containing console logging configuration.</param>
        /// <returns>The updated logger configuration.</returns>
        public static LoggerConfiguration WriteConsoleLogging(
            this LoggerConfiguration loggerConfiguration,
            OpenTelemetrySettings settings)
        {
            var enabled = settings.Logs.Console.Enabled;
            var template = settings.Logs.Console.OutputTemplate;

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
        /// <param name="settings">OpenTelemetry configuration settings.</param>
        /// <param name="serviceName">Logical service name for resource attributes.</param>
        /// <param name="serviceVersion">Service version for resource attributes.</param>
        /// <param name="instanceId">Instance identifier for resource attributes.</param>
        /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
        /// <returns>The updated logger configuration.</returns>
        public static LoggerConfiguration WriteLogsToOpenTelemetry(
            this LoggerConfiguration loggerConfiguration,
            OpenTelemetrySettings settings,
            string serviceName,
            string serviceVersion,
            string instanceId,
            string environmentName)
        {
            var exporter = settings.Logs.Exporter.ToLowerInvariant();

            var configured = loggerConfiguration;
            if (exporter == "otlp")
            {
                var endpoint = settings.Logs.OtlpEndpoint;
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
