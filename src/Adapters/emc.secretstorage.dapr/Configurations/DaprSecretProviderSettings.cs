namespace emc.camus.secretstorage.dapr.Configurations
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
    }
}
