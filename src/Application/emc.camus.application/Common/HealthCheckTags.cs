namespace emc.camus.application.Common;

/// <summary>
/// Defines health check tag constants shared by adapter registrations and endpoint predicates.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>
    /// Tag applied to health checks that participate in the readiness probe.
    /// </summary>
    public const string Ready = "ready";
}
