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
        public string Exporter { get; set; } = "none";

        /// <summary>
        /// Gets or sets the OTLP endpoint for tracing. Defaults to "http://localhost:4317".
        /// </summary>
        public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    }
}
