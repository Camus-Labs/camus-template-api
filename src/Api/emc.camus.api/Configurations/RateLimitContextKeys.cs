using System.Diagnostics.CodeAnalysis;

namespace emc.camus.api.Configurations;

/// <summary>
/// Constants for rate limit context keys stored in HttpContext.Items.
/// </summary>
[ExcludeFromCodeCoverage]
public static class RateLimitContextKeys
{
    /// <summary>
    /// Key for the applied rate limit policy name.
    /// </summary>
    public const string Policy = "RateLimit:Policy";

    /// <summary>
    /// Key for the rate limit permit count.
    /// </summary>
    public const string Limit = "RateLimit:Limit";

    /// <summary>
    /// Key for the rate limit window in seconds.
    /// </summary>
    public const string Window = "RateLimit:Window";
}
