using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using emc.camus.application.Observability;

namespace emc.camus.ratelimiting.inmemory.Metrics
{
    /// <summary>
    /// Provides metrics instrumentation for rate limiting operations.
    /// Exports counters to Prometheus/Application Insights via OpenTelemetry.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RateLimitMetrics
    {
        private const string MetricNameRateLimitRejections = "rate_limit_rejections_total";
        private const string MetricNameUndefinedPolicy = "rate_limit_undefined_policy_total";
        
        private readonly Counter<long> _rateLimitRejectionsCounter;
        private readonly Counter<long> _undefinedPolicyCounter;

        /// <summary>
        /// Creates a new instance of RateLimitMetrics with the specified service name.
        /// </summary>
        /// <param name="serviceName">The service name to use for the meter (should match OpenTelemetry service name).</param>
        public RateLimitMetrics(string serviceName)
        {
            var meter = new Meter($"{serviceName}{MeterNames.Security}");

            // Counter for requests rejected due to rate limiting
            _rateLimitRejectionsCounter = meter.CreateCounter<long>(
                name: MetricNameRateLimitRejections,
                unit: "requests",
                description: "Total number of requests rejected due to rate limiting");

            // Counter for undefined policy usage (configuration issue)
            _undefinedPolicyCounter = meter.CreateCounter<long>(
                name: MetricNameUndefinedPolicy,
                unit: "requests",
                description: "Total number of requests using undefined rate limit policy (fell back to default)");
        }

        /// <summary>
        /// Records a request rejected due to rate limiting.
        /// </summary>
        /// <param name="policyName">The rate limit policy that was exceeded.</param>
        /// <param name="method">The HTTP method.</param>
        public void RecordRejection(string policyName, string method)
        {
            _rateLimitRejectionsCounter.Add(1,
                new KeyValuePair<string, object?>("policy", policyName),
                new KeyValuePair<string, object?>("method", method));
        }

        /// <summary>
        /// Records when an endpoint uses an undefined policy name.
        /// This indicates a configuration issue where the controller attribute references a non-existent policy.
        /// </summary>
        /// <param name="requestedPolicy">The policy name that was requested but not found.</param>
        /// <param name="endpoint">The endpoint path that requested the policy.</param>
        public void RecordUndefinedPolicy(string requestedPolicy, string endpoint)
        {
            _undefinedPolicyCounter.Add(1,
                new KeyValuePair<string, object?>("requested_policy", requestedPolicy),
                new KeyValuePair<string, object?>("endpoint", endpoint));
        }
    }
}
