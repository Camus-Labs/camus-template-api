using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using emc.camus.application.Observability;

namespace emc.camus.security.apikey.Metrics
{
    /// <summary>
    /// Provides metrics instrumentation for API Key authentication operations.
    /// Exports counters to Prometheus/Application Insights via OpenTelemetry.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ApiKeyMetrics
    {
        private readonly Counter<long> _authenticationFailuresCounter;

        /// <summary>
        /// Creates a new instance of ApiKeyMetrics with the specified service name.
        /// </summary>
        /// <param name="serviceName">The service name to use for the meter (should match OpenTelemetry service name).</param>
        public ApiKeyMetrics(string serviceName)
        {
            var meter = new Meter($"{serviceName}{MeterNames.Security}");

            // Counter for API Key authentication failures
            _authenticationFailuresCounter = meter.CreateCounter<long>(
                name: "apikey_authentication_failures_total",
                unit: "requests",
                description: "Total number of API Key authentication failures");
        }

        /// <summary>
        /// Records an API Key authentication failure.
        /// </summary>
        /// <param name="failureReason">The reason for authentication failure (e.g., "missing_header", "invalid_key").</param>
        /// <param name="endpoint">The endpoint path that was accessed.</param>
        public void RecordAuthenticationFailure(string failureReason, string endpoint)
        {
            _authenticationFailuresCounter.Add(1,
                new KeyValuePair<string, object?>("failure_reason", failureReason),
                new KeyValuePair<string, object?>("endpoint", endpoint));
        }
    }
}
