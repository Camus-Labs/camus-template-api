using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace emc.camus.observability.otel.Logging;

/// <summary>
/// Host-level extensions to configure Serilog entirely within the observability adapter,
/// keeping Serilog dependencies out of the API project.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Wires Serilog sinks and enrichers using adapter-provided configuration methods.
    /// </summary>
    /// <param name="host">The host builder.</param>
    /// <param name="configuration">Application configuration (used by sinks).</param>
    /// <param name="environment">Hosting environment (passed through to OTLP setup).</param>
    /// <param name="serviceName">Service name for telemetry.</param>
    /// <param name="serviceVersion">Service version for telemetry.</param>
    /// <returns>The same host builder instance for chaining.</returns>
    public static IHostBuilder UseEmcSerilog(
        this IHostBuilder host,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName,
        string serviceVersion)
    {
        host.UseSerilog((ctx, services, lc) =>
        {
            lc.ApplyDefaultEnrichers()
              .WriteConsoleLogging(configuration)
              .WriteLogsToOpenTelemetry(configuration, environment, serviceName, serviceVersion);
        });

        return host;
    }
}
