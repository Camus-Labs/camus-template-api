using emc.camus.application.Secrets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace emc.camus.secrets.dapr.Services;

/// <summary>
/// Health check that verifies the Dapr secret store is accessible
/// by delegating to the secret provider's connectivity check.
/// </summary>
internal sealed class DaprSecretHealthCheck : IHealthCheck
{
    private readonly ISecretProvider _secretProvider;

    /// <summary>
    /// Creates the health check with the specified secret provider.
    /// </summary>
    /// <param name="secretProvider">The secret provider used to verify store connectivity.</param>
    public DaprSecretHealthCheck(ISecretProvider secretProvider)
    {
        ArgumentNullException.ThrowIfNull(secretProvider);

        _secretProvider = secretProvider;
    }

    /// <summary>
    /// Checks Dapr secret store accessibility by delegating to the provider's connectivity check.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Healthy if the secret store is reachable, Unhealthy otherwise.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _secretProvider.CheckConnectivityAsync(ct);

            return HealthCheckResult.Healthy("Dapr secret store is reachable");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Dapr secret store is unreachable", ex);
        }
    }
}
