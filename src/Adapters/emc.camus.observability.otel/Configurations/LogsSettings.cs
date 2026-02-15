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
        /// Gets or sets the exporter type. Defaults to None.
        /// </summary>
        public LogsExporter Exporter { get; set; } = LogsExporter.None;

        /// <summary>
        /// Gets or sets the OTLP endpoint for logs. Defaults to the OTLP default endpoint.
        /// </summary>
        public string OtlpEndpoint { get; set; } = OtlpDefaults.DefaultEndpoint;

        /// <summary>
        /// Validates the logs configuration.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateConsole();
            ValidateExporter();
            ValidateOtlpEndpoint();
        }

        private void ValidateConsole()
        {
            if (Console == null)
                throw new ArgumentException("Console settings cannot be null", nameof(Console));
            
            Console.Validate();
        }

        private void ValidateExporter()
        {
            if (!Enum.IsDefined(typeof(LogsExporter), Exporter))
                throw new ArgumentException($"Invalid Exporter value: {Exporter}. Valid values are: {string.Join(", ", Enum.GetNames(typeof(LogsExporter)))}", nameof(Exporter));
        }

        private void ValidateOtlpEndpoint()
        {
            if (Exporter == LogsExporter.Otlp)
            {
                if (string.IsNullOrWhiteSpace(OtlpEndpoint))
                    throw new ArgumentException("OtlpEndpoint cannot be null or empty when Exporter is 'otlp'", nameof(OtlpEndpoint));

                if (!Uri.TryCreate(OtlpEndpoint, UriKind.Absolute, out _))
                    throw new ArgumentException($"OtlpEndpoint must be a valid URI: '{OtlpEndpoint}'", nameof(OtlpEndpoint));
            }
        }
    }
}
