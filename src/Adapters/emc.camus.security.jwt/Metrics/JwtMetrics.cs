using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using emc.camus.application.Generic;
using emc.camus.application.Observability;

namespace emc.camus.security.jwt.Metrics
{
    /// <summary>
    /// Provides metrics instrumentation for JWT authentication operations.
    /// Exports counters to Prometheus/Application Insights via OpenTelemetry.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class JwtMetrics
    {
        private const string MetricNameAuthenticationFailures = "jwt_authentication_failures_total";
        
        private readonly Counter<long> _authenticationFailuresCounter;

        /// <summary>
        /// Creates a new instance of JwtMetrics with the specified service name.
        /// </summary>
        /// <param name="serviceName">The service name to use for the meter (should match OpenTelemetry service name).</param>
        public JwtMetrics(string serviceName)
        {
            var meter = new Meter($"{serviceName}{MeterNames.Security}");

            // Counter for JWT authentication failures
            _authenticationFailuresCounter = meter.CreateCounter<long>(
                name: MetricNameAuthenticationFailures,
                unit: "requests",
                description: "Total number of JWT authentication failures");
        }

        /// <summary>
        /// Records a JWT authentication failure.
        /// </summary>
        /// <param name="errorCode">The error code representing the failure reason.</param>
        /// <param name="endpoint">The endpoint path that was accessed.</param>
        public void RecordAuthenticationFailure(string errorCode, string endpoint)
        {
            _authenticationFailuresCounter.Add(1,
                new KeyValuePair<string, object?>("failure_reason", errorCode),
                new KeyValuePair<string, object?>("endpoint", endpoint));
        }
    }
}
