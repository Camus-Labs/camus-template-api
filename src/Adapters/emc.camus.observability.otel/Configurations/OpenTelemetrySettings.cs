namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Configuration settings for OpenTelemetry observability.
    /// </summary>
    public class OpenTelemetrySettings
    {
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
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        public void Validate()
        {
            if (Tracing == null)
                throw new ArgumentException("Tracing settings cannot be null", nameof(Tracing));

            if (Metrics == null)
                throw new ArgumentException("Metrics settings cannot be null", nameof(Metrics));

            if (Logs == null)
                throw new ArgumentException("Logs settings cannot be null", nameof(Logs));

            // Validate each subsection
            Tracing.Validate();
            Metrics.Validate();
            Logs.Validate();
        }
    }
}
