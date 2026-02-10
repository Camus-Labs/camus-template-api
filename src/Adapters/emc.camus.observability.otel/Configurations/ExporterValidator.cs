using System.Diagnostics.CodeAnalysis;

namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Provides shared validation logic for OpenTelemetry exporter configuration.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ExporterValidator
    {
        /// <summary>
        /// Validates that the exporter value is not null/empty and is one of the valid types.
        /// </summary>
        /// <param name="exporter">The exporter value to validate.</param>
        /// <param name="parameterName">The parameter name for error messages.</param>
        /// <exception cref="ArgumentException">Thrown when exporter is invalid.</exception>
        public static void ValidateExporter(string exporter, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(exporter))
                throw new ArgumentException("Exporter cannot be null or empty", parameterName);

            if (!ExporterTypes.Valid.Contains(exporter.ToLowerInvariant()))
                throw new ArgumentException(
                    $"Invalid Exporter value '{exporter}'. Valid values are: {string.Join(", ", ExporterTypes.Valid)}", 
                    parameterName);
        }

        /// <summary>
        /// Validates the OTLP endpoint when the exporter is set to OTLP.
        /// </summary>
        /// <param name="endpoint">The OTLP endpoint to validate.</param>
        /// <param name="exporter">The exporter type.</param>
        /// <param name="endpointParameterName">The parameter name for endpoint error messages.</param>
        /// <exception cref="ArgumentException">Thrown when endpoint is invalid and exporter is OTLP.</exception>
        public static void ValidateOtlpEndpoint(string endpoint, string exporter, string endpointParameterName)
        {
            if (exporter.Equals(ExporterTypes.Otlp, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                    throw new ArgumentException("OtlpEndpoint cannot be null or empty when Exporter is 'otlp'", endpointParameterName);

                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
                    throw new ArgumentException($"OtlpEndpoint must be a valid URI: '{endpoint}'", endpointParameterName);
            }
        }
    }
}
