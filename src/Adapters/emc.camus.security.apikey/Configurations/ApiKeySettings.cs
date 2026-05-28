namespace emc.camus.security.apikey.Configurations
{
    /// <summary>
    /// Configuration settings for API Key authentication.
    /// </summary>
    internal sealed class ApiKeySettings
    {
        /// <summary>
        /// The configuration section name for API Key settings.
        /// </summary>
        public const string ConfigurationSectionName = "ApiKeySettings";

        /// <summary>
        /// Default username assigned to API Key authenticated requests.
        /// </summary>
        public const string DefaultUsername = "ApiKeyUser";

        /// <summary>
        /// Deterministic user ID assigned to API Key authenticated requests.
        /// Ensures idempotency cache entries are scoped per principal.
        /// </summary>
        public const string DefaultUserId = "00000000-0000-0000-0000-000000000001";

        private const string DefaultApiKeySecretName = "XApiKey";

        /// <summary>
        /// Secret name for the API key used in authentication.
        /// </summary>
        public string ApiKeySecretName { get; set; } = DefaultApiKeySecretName;

        /// <summary>
        /// Validates the API Key settings configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateApiKeySecretName();
        }

        private void ValidateApiKeySecretName()
        {
            if (string.IsNullOrWhiteSpace(ApiKeySecretName))
            {
                throw new InvalidOperationException("ApiKeySecretName cannot be null or empty.");
            }
        }
    }
}
