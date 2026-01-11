using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Serilog;
using System;

namespace emc.camus.observability.otel.Logging;

/// <summary>
/// Host-level extensions to configure Serilog entirely within the observability adapter,
/// keeping Serilog dependencies out of the API project.
/// </summary>
public static class HostBuilderExtensions
{
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
