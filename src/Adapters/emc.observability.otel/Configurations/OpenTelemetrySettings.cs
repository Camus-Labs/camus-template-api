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
    }
}
