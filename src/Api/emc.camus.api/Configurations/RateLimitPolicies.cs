using System.Diagnostics.CodeAnalysis;

namespace emc.camus.api.Configurations;

/// <summary>
/// Defines standard rate limit policy names used throughout the API layer.
/// These constants ensure type safety when applying rate limit attributes.
/// Each policy must be configured in appsettings.json under RateLimitingSettings.Policies.
/// </summary>
[ExcludeFromCodeCoverage]
public static class RateLimitPolicies
{
    /// <summary>
    /// Default rate limit policy applied when no [RateLimit] attribute is specified.
    /// </summary>
    public const string Default = "default";

    /// <summary>
    /// Strict rate limit policy for sensitive endpoints.
    /// </summary>
    public const string Strict = "strict";

    /// <summary>
    /// Relaxed rate limit policy for high-throughput endpoints.
    /// </summary>
    public const string Relaxed = "relaxed";

    /// <summary>
    /// Gets all valid rate limit policy names.
    /// </summary>
    /// <returns>Array of all valid policy names.</returns>
    public static string[] GetAll()
    {
        return new[] { Default, Strict, Relaxed };
    }
}
