namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Tracing exporter types for distributed tracing.
    /// </summary>
    internal enum TracingExporter
    {
        /// <summary>
        /// OTLP exporter (OpenTelemetry Protocol).
        /// </summary>
        Otlp,

        /// <summary>
        /// Console exporter (outputs to console).
        /// </summary>
        Console,

        /// <summary>
        /// No exporter (disables export).
        /// </summary>
        None
    }

    /// <summary>
    /// Metrics exporter types for application metrics.
    /// </summary>
    internal enum MetricsExporter
    {
        /// <summary>
        /// OTLP exporter (OpenTelemetry Protocol).
        /// </summary>
        Otlp,

        /// <summary>
        /// Console exporter (outputs to console).
        /// </summary>
        Console,

        /// <summary>
        /// No exporter (disables export).
        /// </summary>
        None
    }

    /// <summary>
    /// Logs exporter types for structured logging.
    /// </summary>
    internal enum LogsExporter
    {
        /// <summary>
        /// OTLP exporter (OpenTelemetry Protocol).
        /// </summary>
        Otlp,

        /// <summary>
        /// Console exporter (outputs to console).
        /// </summary>
        Console,

        /// <summary>
        /// No exporter (disables export).
        /// </summary>
        None
    }
}
