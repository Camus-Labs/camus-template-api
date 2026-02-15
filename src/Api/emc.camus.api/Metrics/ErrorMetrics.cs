using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using emc.camus.application.Observability;

namespace emc.camus.api.Metrics
{
    /// <summary>
    /// Provides centralized metrics instrumentation for error handling operations.
    /// Tracks all application errors by error code, HTTP status, and endpoint path.
    /// Exports counters to Prometheus/Application Insights via OpenTelemetry.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ErrorMetrics
    {
        private const string MetricNameErrorResponses = "error_responses_total";
        
        private readonly Counter<long> _errorResponsesCounter;
        private readonly ILogger<ErrorMetrics> _logger;

        /// <summary>
        /// Creates a new instance of ErrorMetrics with the specified service name.
        /// </summary>
        /// <param name="serviceName">The service name to use for the meter (should match OpenTelemetry service name).</param>
        /// <param name="logger">Logger for recording telemetry failures.</param>
        public ErrorMetrics(string serviceName, ILogger<ErrorMetrics> logger)
        {
            var meter = new Meter($"{serviceName}{MeterNames.ErrorHandling}");

            // Counter for all error responses
            _errorResponsesCounter = meter.CreateCounter<long>(
                name: MetricNameErrorResponses,
                unit: "responses",
                description: "Total number of error responses returned by the application");
            
            _logger = logger;
        }

        /// <summary>
        /// Records an error response with its associated metadata.
        /// This method is fire-and-forget - any exceptions are suppressed to prevent telemetry failures from affecting application behavior.
        /// </summary>
        /// <param name="errorCode">The application error code (e.g., "jwt_token_expired", "rate_limit_exceeded").</param>
        /// <param name="httpStatus">The HTTP status code returned (e.g., 401, 429, 500).</param>
        /// <param name="path">The endpoint path that generated the error.</param>
        public void RecordError(string errorCode, int httpStatus, string path)
        {
            try
            {
                _errorResponsesCounter.Add(1,
                    new KeyValuePair<string, object?>("error_code", errorCode),
                    new KeyValuePair<string, object?>("http_status", httpStatus),
                    new KeyValuePair<string, object?>("path", path));
            }
            catch (Exception ex)
            {
                // Log but suppress - telemetry failures should never affect application behavior
                _logger.LogWarning(ex, "Failed to record error metrics for error code {ErrorCode}", errorCode);
            }
        }
    }
}
