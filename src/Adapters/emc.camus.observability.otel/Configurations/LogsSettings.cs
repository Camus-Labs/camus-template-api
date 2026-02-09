namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Configuration settings for OpenTelemetry logs.
    /// </summary>
    public class LogsSettings
    {
        private static readonly string[] ValidExporters = { "otlp", "console", "none" };

        /// <summary>
        /// Gets or sets the console logging configuration.
        /// </summary>
        public ConsoleSettings Console { get; set; } = new();

        /// <summary>
        /// Gets or sets the exporter type. Valid values: "otlp", "console", "none". Defaults to "none".
        /// </summary>
        public string Exporter { get; set; } = "none";

        /// <summary>
        /// Gets or sets the OTLP endpoint for logs. Defaults to "http://localhost:4317".
        /// </summary>
        public string OtlpEndpoint { get; set; } = "http://localhost:4317";

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

            if (string.IsNullOrWhiteSpace(Exporter))
                throw new ArgumentException("Exporter cannot be null or empty", nameof(Exporter));

            if (!ValidExporters.Contains(Exporter.ToLowerInvariant()))
                throw new ArgumentException(
                    $"Invalid Exporter value '{Exporter}'. Valid values are: {string.Join(", ", ValidExporters)}", 
                    nameof(Exporter));

            if (Exporter.Equals("otlp", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(OtlpEndpoint))
                    throw new ArgumentException("OtlpEndpoint cannot be null or empty when Exporter is 'otlp'", nameof(OtlpEndpoint));

                if (!Uri.TryCreate(OtlpEndpoint, UriKind.Absolute, out _))
                    throw new ArgumentException($"OtlpEndpoint must be a valid URI: '{OtlpEndpoint}'", nameof(OtlpEndpoint));
            }
        }
    }
}
