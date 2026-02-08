namespace emc.camus.security.apikey.Configurations
{
    /// <summary>
    /// Configuration settings for API Key authentication.
    /// </summary>
    public class ApiKeySettings
    {
        /// <summary>
        /// Gets or sets the name of the secret key used to retrieve the API key from the secret provider.
        /// Defaults to "XApiKey".
        /// </summary>
        public string SecretKeyName { get; set; } = "XApiKey";
    }
}
