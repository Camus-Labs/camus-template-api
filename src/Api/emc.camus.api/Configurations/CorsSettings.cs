namespace emc.camus.api.Configurations
{
    /// <summary>
    /// Configuration settings for CORS policy.
    /// </summary>
    public class CorsSettings
    {
        /// <summary>
        /// Gets or sets the name of the CORS policy.
        /// </summary>
        public string PolicyName { get; set; } = "DefaultCorsPolicy";

        /// <summary>
        /// Gets or sets the allowed origins for CORS requests.
        /// </summary>
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the allowed HTTP methods for CORS requests.
        /// </summary>
        public string[] AllowedMethods { get; set; } = new[] { "GET", "POST" };

        /// <summary>
        /// Gets or sets the allowed headers for CORS requests.
        /// </summary>
        public string[] AllowedHeaders { get; set; } = new[] { "Content-Type", "Authorization", "X-Api-Key" };

        /// <summary>
        /// Gets or sets the headers that should be exposed to the client.
        /// </summary>
        public string[] ExposedHeaders { get; set; } = new[] { "Content-Type", "X-Trace-Id"  };

        /// <summary>
        /// Gets or sets whether credentials are allowed in CORS requests.
        /// </summary>
        public bool AllowCredentials { get; set; } = false;

        /// <summary>
        /// Gets or sets the preflight cache duration in minutes.
        /// </summary>
        public int PreflightMaxAgeMinutes { get; set; } = 60;
    }
}
