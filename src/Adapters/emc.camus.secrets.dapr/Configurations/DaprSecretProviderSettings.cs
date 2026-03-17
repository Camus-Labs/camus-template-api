namespace emc.camus.secrets.dapr.Configurations
{
    /// <summary>
    /// Configuration settings for the Dapr secret provider.
    /// </summary>
    public class DaprSecretProviderSettings
    {
        /// <summary>
        /// The configuration section name for Dapr secret provider settings.
        /// </summary>
        public const string ConfigurationSectionName = "DaprSecretProviderSettings";

        private const int MinPort = 1;
        private const int MaxPort = 65535;
        private const int MaxTimeoutSeconds = 300;

        /// <summary>
        /// Gets or sets the base URL for the Dapr sidecar. Defaults to "localhost".
        /// </summary>
        public string BaseHost { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the HTTP port for the Dapr sidecar. Defaults to "3500".
        /// </summary>
        public string HttpPort { get; set; } = "3500";

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
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateBaseHost();
            ValidateHttpPort();
            ValidateSecretStoreName();
            ValidateTimeoutSeconds();
            ValidateSecretNames();
        }

        private void ValidateBaseHost()
        {
            if (string.IsNullOrWhiteSpace(BaseHost))
                throw new InvalidOperationException("BaseHost cannot be null or empty");
        }

        private void ValidateHttpPort()
        {
            if (string.IsNullOrWhiteSpace(HttpPort))
                throw new InvalidOperationException("HttpPort cannot be null or empty");

            if (!int.TryParse(HttpPort, out int portNumber) || portNumber < MinPort || portNumber > MaxPort)
                throw new InvalidOperationException($"HttpPort must be a valid port number ({MinPort}-{MaxPort}): '{HttpPort}'");
        }

        private void ValidateSecretStoreName()
        {
            if (string.IsNullOrWhiteSpace(SecretStoreName))
                throw new InvalidOperationException("SecretStoreName cannot be null or empty");
        }

        private void ValidateTimeoutSeconds()
        {
            if (TimeoutSeconds <= 0 || TimeoutSeconds > MaxTimeoutSeconds)
                throw new InvalidOperationException($"TimeoutSeconds must be between 1 and {MaxTimeoutSeconds}");
        }

        private void ValidateSecretNames()
        {
            if (SecretNames == null)
                throw new InvalidOperationException("SecretNames cannot be null");

            if (SecretNames.Count == 0)
                throw new InvalidOperationException("At least one secret name must be specified in SecretNames");

            if (SecretNames.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("SecretNames cannot contain null or empty values");
        }
    }
}
