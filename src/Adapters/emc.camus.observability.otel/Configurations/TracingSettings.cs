namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Configuration settings for OpenTelemetry tracing.
    /// </summary>
    public class TracingSettings
    {

        /// <summary>
        /// Gets or sets the exporter type. Valid values: "otlp", "console", "none". Defaults to "none".
        /// </summary>
        public string Exporter { get; set; } = ExporterTypes.None;

        /// <summary>
        /// Gets or sets the OTLP endpoint for tracing. Defaults to the OTLP default endpoint.
        /// </summary>
        public string OtlpEndpoint { get; set; } = OtlpDefaults.DefaultEndpoint;

        /// <summary>
        /// Validates the tracing configuration.
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        public void Validate()
        {
            ExporterValidator.ValidateExporter(Exporter, nameof(Exporter));
            ExporterValidator.ValidateOtlpEndpoint(OtlpEndpoint, Exporter, nameof(OtlpEndpoint));
        }
    }
}
