using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Serilog;
using System;

namespace emc.camus.observability.otel.Logging;

/// <summary>
/// Host-level extensions to configure Serilog logging for .NET applications using the Camus observability adapter.
/// This keeps Serilog dependencies out of the API project and enables structured logging with OpenTelemetry integration.
/// Supported log exporters: OTLP (default, via Collector), Loki (via Collector), Console.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog for the host using Camus observability conventions.
    /// Applies default enrichers, console logging, and OpenTelemetry log export.
    /// 
    /// <param name="host">The host builder to configure.</param>
    /// <param name="configuration">Application configuration (expects OpenTelemetry:Logs section).</param>
    /// <param name="serviceName">Logical service name for resource attributes.</param>
    /// <param name="serviceVersion">Service version for resource attributes.</param>
    /// <param name="instanceId">Instance identifier for resource attributes.</param>
    /// <param name="environmentName">Environment name (e.g., Development, Production).</param>
    /// <returns>The configured host builder.</returns>
    /// </summary>
    public static IHostBuilder UseEmcSerilog(
        this IHostBuilder host,
        IConfiguration configuration,
        string serviceName,
        string serviceVersion,
        string instanceId,
        string environmentName)
    {
        host.UseSerilog((ctx, services, lc) =>
        {
            lc.ApplyDefaultEnrichers()
                .WriteConsoleLogging(configuration)
                .WriteLogsToOpenTelemetry(configuration, serviceName, serviceVersion, instanceId, environmentName);
        });

        return host;
    }
}
