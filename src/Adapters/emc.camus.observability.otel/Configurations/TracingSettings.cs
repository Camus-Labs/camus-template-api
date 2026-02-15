namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Configuration settings for OpenTelemetry tracing.
    /// </summary>
    public class TracingSettings
    {

        /// <summary>
        /// Gets or sets the exporter type. Defaults to None.
        /// </summary>
        public TracingExporter Exporter { get; set; } = TracingExporter.None;

        /// <summary>
        /// Gets or sets the OTLP endpoint for tracing. Defaults to the OTLP default endpoint.
        /// </summary>
        public string OtlpEndpoint { get; set; } = OtlpDefaults.DefaultEndpoint;

        /// <summary>
        /// Validates the tracing configuration.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateExporter();
            ValidateOtlpEndpoint();
        }

        private void ValidateExporter()
        {
            if (!Enum.IsDefined(typeof(TracingExporter), Exporter))
                throw new ArgumentException($"Invalid Exporter value: {Exporter}. Valid values are: {string.Join(", ", Enum.GetNames(typeof(TracingExporter)))}", nameof(Exporter));
        }

        private void ValidateOtlpEndpoint()
        {
            if (Exporter == TracingExporter.Otlp)
            {
                if (string.IsNullOrWhiteSpace(OtlpEndpoint))
                    throw new ArgumentException("OtlpEndpoint cannot be null or empty when Exporter is 'otlp'", nameof(OtlpEndpoint));

                if (!Uri.TryCreate(OtlpEndpoint, UriKind.Absolute, out _))
                    throw new ArgumentException($"OtlpEndpoint must be a valid URI: '{OtlpEndpoint}'", nameof(OtlpEndpoint));
            }
        }
    }
}
