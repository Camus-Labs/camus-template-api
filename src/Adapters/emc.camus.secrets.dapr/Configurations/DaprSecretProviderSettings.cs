namespace emc.camus.secrets.dapr.Configurations
{
    /// <summary>
    /// Configuration settings for the Dapr secret provider.
    /// </summary>
    public class DaprSecretProviderSettings
    {
        /// <summary>
        /// Gets or sets the base URL for the Dapr sidecar. Defaults to "localhost".
        /// </summary>
        public string BaseHost { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the HTTP port for the Dapr sidecar. Defaults to "3500".
        /// </summary>
        public string HttpPort { get; set; } = "3500";

        /// <summary>
        /// Gets or sets whether to use HTTPS for Dapr sidecar communication. Defaults to false.
        /// </summary>
        public bool UseHttps { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of the Dapr secret store. Defaults to "default-secret-store".
        /// </summary>
        public string SecretStoreName { get; set; } = "default-secret-store";

        /// <summary>
        /// Gets or sets the HTTP request timeout in seconds. Defaults to 30 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the list of secret names to load at startup.
        /// </summary>
        public List<string> SecretNames { get; set; } = new();

        /// <summary>
        /// Validates the Dapr secret provider settings configuration.
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(BaseHost))
                throw new ArgumentException("BaseHost cannot be null or empty", nameof(BaseHost));

            if (string.IsNullOrWhiteSpace(HttpPort))
                throw new ArgumentException("HttpPort cannot be null or empty", nameof(HttpPort));

            // Validate HttpPort is a valid port number
            if (!int.TryParse(HttpPort, out int portNumber) || portNumber < 1 || portNumber > 65535)
                throw new ArgumentException($"HttpPort must be a valid port number (1-65535): '{HttpPort}'", nameof(HttpPort));

            if (string.IsNullOrWhiteSpace(SecretStoreName))
                throw new ArgumentException("SecretStoreName cannot be null or empty", nameof(SecretStoreName));

            if (TimeoutSeconds <= 0 || TimeoutSeconds > 300)
                throw new ArgumentException("TimeoutSeconds must be between 1 and 300", nameof(TimeoutSeconds));

            if (SecretNames == null)
                throw new ArgumentException("SecretNames cannot be null", nameof(SecretNames));

            if (SecretNames.Count == 0)
                throw new ArgumentException("At least one secret name must be specified in SecretNames", nameof(SecretNames));

            // Validate each secret name
            foreach (var secretName in SecretNames)
            {
                if (string.IsNullOrWhiteSpace(secretName))
                    throw new ArgumentException("SecretNames cannot contain null or empty values", nameof(SecretNames));
            }
        }
    }
}
