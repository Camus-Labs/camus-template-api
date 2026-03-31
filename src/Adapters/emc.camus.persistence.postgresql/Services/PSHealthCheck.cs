using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace emc.camus.persistence.postgresql.Services;

/// <summary>
/// Health check that verifies PostgreSQL database connectivity by opening a connection
/// through the unit of work.
/// </summary>
internal sealed class PSHealthCheck : IHealthCheck
{
    private readonly PSUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates the health check with the specified unit of work.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to verify database connectivity.</param>
    public PSHealthCheck(PSUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Checks PostgreSQL connectivity by obtaining a connection through the unit of work.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Healthy if the database is reachable, Unhealthy otherwise.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.GetConnectionAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL database is unreachable", ex);
        }
    }
}
