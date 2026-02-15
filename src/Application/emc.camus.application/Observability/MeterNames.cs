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
        /// Business domain metrics (orders, users, transactions)
        /// </summary>
        public const string Business = ".business";

        /// <summary>
        /// Security metrics (rate limiting, others)
        /// </summary>
        public const string Security = ".security";

        /// <summary>
        /// Infrastructure metrics (database, cache, external APIs, message queues)
        /// </summary>
        public const string Infrastructure = ".infrastructure";

        /// <summary>
        /// Error handling and exception tracking metrics
        /// </summary>
        public const string ErrorHandling = ".errorhandling";

        /// <summary>
        /// Returns all meter name suffixes defined in the application.
        /// </summary>
        /// <returns>Array of all meter name suffixes</returns>
        public static string[] GetAll()
        {
            return new[]
            {
                Application,
                Business,
                Security,
                Infrastructure,
                ErrorHandling
            };
        }
    }
}
