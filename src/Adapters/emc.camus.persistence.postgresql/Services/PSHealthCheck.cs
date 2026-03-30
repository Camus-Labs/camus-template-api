using emc.camus.application.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace emc.camus.persistence.postgresql.Services;

/// <summary>
/// Health check that verifies PostgreSQL database connectivity by opening a connection
/// through the existing connection factory.
/// </summary>
internal sealed class PSHealthCheck : IHealthCheck
{
    private readonly IConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates the health check with the specified connection factory.
    /// </summary>
    /// <param name="connectionFactory">The connection factory used to verify database connectivity.</param>
    public PSHealthCheck(IConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Checks PostgreSQL connectivity by creating a connection through the factory.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Healthy if the database is reachable, Unhealthy otherwise.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL database is unreachable", ex);
        }
    }
}
