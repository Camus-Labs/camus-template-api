namespace emc.camus.main.api.Models
{
    /// <summary>
    /// Request model for JWT token generation.
    /// </summary>
    public class JwtTokenRequest
    {
        /// <summary>
        /// The access key for authentication.
        /// </summary>
        public string? AccessKey { get; set; }

        /// <summary>
        /// The access secret for authentication.
        /// </summary>
        public string? AccessSecret { get; set; }
    }

    /// <summary>
    /// Response model for JWT token generation.
    /// </summary>
    public class JwtTokenResponse
    {
        /// <summary>
        /// The generated JWT token.
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// The expiration date and time of the token (UTC).
        /// </summary>
        public DateTime ExpiresOn { get; set; }
    }

    /// <summary>
    /// Model representing API information for documentation and status endpoints.
    /// </summary>
    public class ApiInfo
    {
        /// <summary>
        /// The name of the API.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The version of the API.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// The current status of the API.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// List of features available in this API version.
        /// </summary>
        public List<string>? Features { get; set; }

        /// <summary>
        /// The timestamp of the API status or info (UTC).
        /// </summary>
        public DateTime? Timestamp { get; set; }
    }
}