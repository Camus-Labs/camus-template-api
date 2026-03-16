namespace emc.camus.observability.otel.Configurations
{
    /// <summary>
    /// Configuration settings for console logging.
    /// </summary>
    public class ConsoleSettings
    {
        /// <summary>
        /// Gets or sets whether console logging is enabled. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the output template for console logs. Defaults to a format that includes trace context.
        /// </summary>
        public string OutputTemplate { get; set; } = "[{Timestamp:HH:mm:ss} {Level:u3}] (trace_id={trace_id} span_id={span_id}) {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Validates the console settings configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateOutputTemplate();
        }

        private void ValidateOutputTemplate()
        {
            if (string.IsNullOrWhiteSpace(OutputTemplate))
                throw new InvalidOperationException("OutputTemplate cannot be null or empty");
        }
    }
}
