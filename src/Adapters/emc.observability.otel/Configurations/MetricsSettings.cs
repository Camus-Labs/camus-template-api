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
        public string Exporter { get; set; } = "none";

        /// <summary>
        /// Gets or sets the OTLP endpoint for metrics. Defaults to "http://localhost:4317".
        /// </summary>
        public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    }
}
