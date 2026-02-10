using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Observability
{
    /// <summary>
    /// Defines meter name suffixes for OpenTelemetry metrics instrumentation.
    /// Each meter logically groups related metrics.
    /// Full meter name format: "{ServiceName}{Suffix}" (e.g., "emc.camus.main.api.ratelimiting")
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class MeterNames
    {
        /// <summary>
        /// Base application meter (default - uses service name as-is)
        /// </summary>
        public const string Application = "";

        /// <summary>
        /// Rate limiting metrics (rejections, hits, quotas)
        /// </summary>
        public const string RateLimiting = ".ratelimiting";

        /// <summary>
        /// Business domain metrics (orders, users, transactions)
        /// </summary>
        public const string Business = ".business";

        /// <summary>
        /// Infrastructure metrics (database, cache, external APIs)
        /// </summary>
        public const string Infrastructure = ".infrastructure";

        /// <summary>
        /// Returns all meter name suffixes defined in the application.
        /// Use DisabledMeters configuration to selectively exclude meters.
        /// </summary>
        /// <returns>Array of all meter name suffixes</returns>
        public static string[] GetAll()
        {
            return new[]
            {
                Application,
                RateLimiting,
                Business,
                Infrastructure
            };
        }
    }
}
