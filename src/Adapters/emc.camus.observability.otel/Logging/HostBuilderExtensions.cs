using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Serilog;
using System;
using emc.camus.observability.otel.Configurations;

namespace emc.camus.observability.otel.Logging;

/// <summary>
/// Host-level extensions to configure Serilog logging for .NET applications.
/// This keeps Serilog dependencies out of the API project and enables structured logging with OpenTelemetry integration.
/// Supported log exporters: OTLP (default, via Collector), Loki (via Collector), Console.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog for the host with OpenTelemetry integration.
    /// Applies default enrichers, console logging, and OpenTelemetry log export.
    /// 
    /// <param name="host">The host builder to configure.</param>
    /// <param name="settings">OpenTelemetry settings containing logs exporter configuration.</param>
    /// <param name="serviceName">Logical service name for resource attributes.</param>
    /// <param name="serviceVersion">Service version for resource attributes.</param>
    /// <param name="instanceId">Instance identifier for resource attributes.</param>
    /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
    /// <returns>The configured host builder.</returns>
    /// </summary>
    public static IHostBuilder UseSerilogWithOpenTelemetry(
        this IHostBuilder host,
        OpenTelemetrySettings settings,
        string serviceName,
        string serviceVersion,
        string instanceId,
        string environmentName)
    {
        host.UseSerilog((ctx, services, lc) =>
        {
            lc.ReadFrom.Configuration(ctx.Configuration)
                .ApplyDefaultEnrichers()
                .WriteConsoleLogging(settings)
                .WriteLogsToOpenTelemetry(settings, serviceName, serviceVersion, instanceId, environmentName);
        });

        return host;
    }
}
