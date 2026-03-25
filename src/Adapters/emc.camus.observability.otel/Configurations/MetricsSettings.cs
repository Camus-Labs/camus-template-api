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
        /// <exception cref="InvalidOperationException">
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
            if (!Enum.IsDefined(Exporter))
                throw new InvalidOperationException($"Invalid Exporter value: {Exporter}. Valid values are: {string.Join(", ", Enum.GetNames<MetricsExporter>())}");
        }

        private void ValidateOtlpEndpoint()
        {
            if (Exporter == MetricsExporter.Otlp)
            {
                if (string.IsNullOrWhiteSpace(OtlpEndpoint))
                    throw new InvalidOperationException("OtlpEndpoint cannot be null or empty when Exporter is 'otlp'");

                if (!Uri.TryCreate(OtlpEndpoint, UriKind.Absolute, out _))
                    throw new InvalidOperationException($"OtlpEndpoint must be a valid URI: '{OtlpEndpoint}'");
            }
        }

        private void ValidateDisabledMetrics()
        {
            if (DisabledMetrics == null)
                throw new InvalidOperationException("DisabledMetrics cannot be null");

            if (DisabledMetrics.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("DisabledMetrics cannot contain null or empty entries");
        }

        private void ValidateDisabledMeters()
        {
            if (DisabledMeters == null)
                throw new InvalidOperationException("DisabledMeters cannot be null");

            if (DisabledMeters.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("DisabledMeters cannot contain null or empty entries");
        }
    }
}
