using System.Diagnostics.CodeAnalysis;

namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Defines default values for OTLP (OpenTelemetry Protocol) configuration.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class OtlpDefaults
    {
        /// <summary>
        /// Default OTLP endpoint for traces, metrics, and logs.
        /// Uses gRPC protocol on port 4317.
        /// </summary>
        public const string DefaultEndpoint = "http://localhost:4317";
    }
}
