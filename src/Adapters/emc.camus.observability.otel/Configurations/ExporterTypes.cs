using System.Diagnostics.CodeAnalysis;

namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Defines OpenTelemetry exporter type constants and valid values.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ExporterTypes
    {
        /// <summary>
        /// OTLP exporter (OpenTelemetry Protocol).
        /// </summary>
        public const string Otlp = "otlp";

        /// <summary>
        /// Console exporter (outputs to console).
        /// </summary>
        public const string Console = "console";

        /// <summary>
        /// No exporter (disables export).
        /// </summary>
        public const string None = "none";

        /// <summary>
        /// Array of all valid exporter types.
        /// </summary>
        public static readonly string[] Valid = { Otlp, Console, None };
    }
}
