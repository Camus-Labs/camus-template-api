namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Configuration settings for OpenTelemetry metrics.
    /// </summary>
    public class MetricsSettings
    {

        /// <summary>
        /// Gets or sets the exporter type. Valid values: "otlp", "console", "none". Defaults to "none".
        /// </summary>
        public string Exporter { get; set; } = ExporterTypes.None;

        /// <summary>
        /// Gets or sets the OTLP endpoint for metrics. Defaults to the OTLP default endpoint.
        /// </summary>
        public string OtlpEndpoint { get; set; } = OtlpDefaults.DefaultEndpoint;

        /// <summary>
        /// Gets or sets the list of built-in metric names to disable (e.g., "http.server.duration").
        /// </summary>
        public string[] DisabledMetrics { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the list of custom meter suffixes to disable (e.g., ".infrastructure", ".business").
        /// Meter names are matched as: {ServiceName}{Suffix}
        /// Example: To disable "emc.camus.main.api.infrastructure", add ".infrastructure"
        /// </summary>
        public string[] DisabledMeters { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Validates the metrics configuration.
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        public void Validate()
        {
            ExporterValidator.ValidateExporter(Exporter, nameof(Exporter));
            ExporterValidator.ValidateOtlpEndpoint(OtlpEndpoint, Exporter, nameof(OtlpEndpoint));

            if (DisabledMetrics == null)
                throw new ArgumentException("DisabledMetrics cannot be null", nameof(DisabledMetrics));

            if (DisabledMeters == null)
                throw new ArgumentException("DisabledMeters cannot be null", nameof(DisabledMeters));
        }
    }
}
