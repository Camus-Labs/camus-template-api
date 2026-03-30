using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace emc.camus.secrets.dapr.Services;

/// <summary>
/// Health check that verifies the Dapr secret store is accessible
/// by delegating to the secret provider's connectivity check.
/// </summary>
internal sealed class DaprSecretHealthCheck : IHealthCheck
{
    private readonly DaprSecretProvider _secretProvider;

    /// <summary>
    /// Creates the health check with the specified Dapr secret provider.
    /// </summary>
    /// <param name="secretProvider">The secret provider used to verify store connectivity.</param>
    public DaprSecretHealthCheck(DaprSecretProvider secretProvider)
    {
        ArgumentNullException.ThrowIfNull(secretProvider);

        _secretProvider = secretProvider;
    }

    /// <summary>
    /// Checks Dapr secret store accessibility by delegating to the provider's connectivity check.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Healthy if the secret store is reachable, Unhealthy otherwise.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _secretProvider.CheckConnectivityAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Dapr secret store is unreachable", ex);
        }
    }
}
