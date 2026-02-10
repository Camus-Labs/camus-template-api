namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Configuration settings for OpenTelemetry logs.
    /// </summary>
    public class LogsSettings
    {

        /// <summary>
        /// Gets or sets the console logging configuration.
        /// </summary>
        public ConsoleSettings Console { get; set; } = new();

        /// <summary>
        /// Gets or sets the exporter type. Valid values: "otlp", "console", "none". Defaults to "none".
        /// </summary>
        public string Exporter { get; set; } = ExporterTypes.None;

        /// <summary>
        /// Gets or sets the OTLP endpoint for logs. Defaults to the OTLP default endpoint.
        /// </summary>
        public string OtlpEndpoint { get; set; } = OtlpDefaults.DefaultEndpoint;

        /// <summary>
        /// Validates the logs configuration.
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        public void Validate()
        {
            if (Console == null)
                throw new ArgumentException("Console settings cannot be null", nameof(Console));
            
            // Validate console settings
            Console.Validate();

            ExporterValidator.ValidateExporter(Exporter, nameof(Exporter));
            ExporterValidator.ValidateOtlpEndpoint(OtlpEndpoint, Exporter, nameof(OtlpEndpoint));
        }
    }
}
