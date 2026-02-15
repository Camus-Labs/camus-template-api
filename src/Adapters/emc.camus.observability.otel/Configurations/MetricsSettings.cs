namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Configuration settings for OpenTelemetry metrics.
    /// </summary>
    public class MetricsSettings
    {

        /// <summary>
        /// Gets or sets the exporter type. Defaults to None.
        /// </summary>
        public MetricsExporter Exporter { get; set; } = MetricsExporter.None;

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
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateExporter();
            ValidateOtlpEndpoint();
            ValidateDisabledMetrics();
            ValidateDisabledMeters();
        }

        private void ValidateExporter()
        {
            if (!Enum.IsDefined(typeof(MetricsExporter), Exporter))
                throw new ArgumentException($"Invalid Exporter value: {Exporter}. Valid values are: {string.Join(", ", Enum.GetNames(typeof(MetricsExporter)))}", nameof(Exporter));
        }

        private void ValidateOtlpEndpoint()
        {
            if (Exporter == MetricsExporter.Otlp)
            {
                if (string.IsNullOrWhiteSpace(OtlpEndpoint))
                    throw new ArgumentException("OtlpEndpoint cannot be null or empty when Exporter is 'otlp'", nameof(OtlpEndpoint));

                if (!Uri.TryCreate(OtlpEndpoint, UriKind.Absolute, out _))
                    throw new ArgumentException($"OtlpEndpoint must be a valid URI: '{OtlpEndpoint}'", nameof(OtlpEndpoint));
            }
        }

        private void ValidateDisabledMetrics()
        {
            if (DisabledMetrics == null)
                throw new ArgumentException("DisabledMetrics cannot be null", nameof(DisabledMetrics));
        }

        private void ValidateDisabledMeters()
        {
            if (DisabledMeters == null)
                throw new ArgumentException("DisabledMeters cannot be null", nameof(DisabledMeters));
        }
    }
}
