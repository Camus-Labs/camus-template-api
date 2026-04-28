using emc.camus.application.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace emc.camus.persistence.postgresql.Services;

/// <summary>
/// Health check that verifies PostgreSQL database connectivity by opening a connection
/// through the unit of work.
/// </summary>
internal sealed class HealthCheck : IHealthCheck
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates the health check with the specified unit of work.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to verify database connectivity.</param>
    public HealthCheck(IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Checks PostgreSQL connectivity by obtaining a connection through the unit of work.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Healthy if the database is reachable, Unhealthy otherwise.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await _unitOfWork.CheckConnectivityAsync(ct);

            return HealthCheckResult.Healthy("PostgreSQL database is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL database is unreachable", ex);
        }
    }
}
