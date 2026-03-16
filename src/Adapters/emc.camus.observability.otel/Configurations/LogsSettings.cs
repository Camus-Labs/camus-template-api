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
        /// <exception cref="InvalidOperationException">
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
                throw new InvalidOperationException("Console settings cannot be null");

            Console.Validate();
        }

        private void ValidateExporter()
        {
            if (!Enum.IsDefined(Exporter))
                throw new InvalidOperationException($"Invalid Exporter value: {Exporter}. Valid values are: {string.Join(", ", Enum.GetNames<LogsExporter>())}");
        }

        private void ValidateOtlpEndpoint()
        {
            if (Exporter == LogsExporter.Otlp)
            {
                if (string.IsNullOrWhiteSpace(OtlpEndpoint))
                    throw new InvalidOperationException("OtlpEndpoint cannot be null or empty when Exporter is 'otlp'");

                if (!Uri.TryCreate(OtlpEndpoint, UriKind.Absolute, out _))
                    throw new InvalidOperationException($"OtlpEndpoint must be a valid URI: '{OtlpEndpoint}'");
            }
        }
    }
}
