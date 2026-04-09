using System.Diagnostics.CodeAnalysis;

namespace emc.camus.api.Configurations
{
    /// <summary>
    /// Defines standard request timeout policy names for use with [RequestTimeout] attribute.
    /// Backed by ASP.NET Core built-in request timeouts (Microsoft.AspNetCore.Http.Timeouts).
    /// Each policy is registered in RequestTimeoutSetupExtensions.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class RequestTimeoutPolicies
    {
        /// <summary>
        /// Default timeout for standard endpoints.
        /// Use to override a class-level policy on a specific action.
        /// Default: 30 seconds.
        /// </summary>
        public const string Default = "default";

        /// <summary>
        /// Tight timeout for fast endpoints (simple reads, health-adjacent).
        /// Default: 10 seconds.
        /// </summary>
        public const string Tight = "tight";

        /// <summary>
        /// Extended timeout for slow endpoints (bulk operations, report generation).
        /// Default: 60 seconds.
        /// </summary>
        public const string Extended = "extended";
    }
}
