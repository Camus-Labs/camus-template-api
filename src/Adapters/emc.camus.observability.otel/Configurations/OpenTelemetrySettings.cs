namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Configuration settings for OpenTelemetry observability.
    /// </summary>
    internal sealed class OpenTelemetrySettings
    {
        /// <summary>
        /// The configuration section name for OpenTelemetry settings.
        /// </summary>
        public const string ConfigurationSectionName = "OpenTelemetrySettings";

        /// <summary>
        /// Gets or sets the tracing configuration.
        /// </summary>
        public TracingSettings Tracing { get; set; } = new();

        /// <summary>
        /// Gets or sets the metrics configuration.
        /// </summary>
        public MetricsSettings Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets the logs configuration.
        /// </summary>
        public LogsSettings Logs { get; set; } = new();

        /// <summary>
        /// Validates the OpenTelemetry configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateTracing();
            ValidateMetrics();
            ValidateLogs();
        }

        private void ValidateTracing()
        {
            if (Tracing == null)
                throw new InvalidOperationException("Tracing settings cannot be null");

            Tracing.Validate();
        }

        private void ValidateMetrics()
        {
            if (Metrics == null)
                throw new InvalidOperationException("Metrics settings cannot be null");

            Metrics.Validate();
        }

        private void ValidateLogs()
        {
            if (Logs == null)
                throw new InvalidOperationException("Logs settings cannot be null");

            Logs.Validate();
        }
    }
}
