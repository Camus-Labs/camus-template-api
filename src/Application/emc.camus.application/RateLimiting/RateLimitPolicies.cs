using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.RateLimiting
{
    /// <summary>
    /// Defines standard rate limit policy names used throughout the application.
    /// These constants ensure type safety when applying rate limit attributes.
    /// Each policy must be configured in appsettings.json under RateLimitSettings.Policies.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class RateLimitPolicies
    {
        /// <summary>
        /// Default rate limit policy applied when no [RateLimit] attribute is specified.
        /// Recommended: 100-1000 requests per minute for general API usage.
        /// </summary>
        public const string Default = "default";

        /// <summary>
        /// Strict rate limit policy for sensitive endpoints (e.g., authentication, password reset).
        /// Recommended: 3-10 requests per minute to prevent brute force attacks.
        /// </summary>
        public const string Strict = "strict";

        /// <summary>
        /// Relaxed rate limit policy for high-throughput endpoints (e.g., read-heavy operations, bulk queries).
        /// Recommended: 1000-5000 requests per minute for performance-intensive operations.
        /// </summary>
        public const string Relaxed = "relaxed";
    }
}
