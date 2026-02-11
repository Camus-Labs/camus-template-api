using emc.camus.application.Generic;
using Microsoft.Net.Http.Headers;

namespace emc.camus.api.Configurations
{
    /// <summary>
    /// Configuration settings for CORS policy.
    /// </summary>
    public class CorsSettings
    {
        /// <summary>
        /// The configuration section name for CORS settings.
        /// </summary>
        public const string ConfigurationSectionName = "CorsSettings";
        
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
        public string[] AllowedHeaders { get; set; } = new[] { HeaderNames.ContentType, HeaderNames.Authorization, Headers.ApiKey };

        /// <summary>
        /// Gets or sets the headers that should be exposed to the client.
        /// </summary>
        public string[] ExposedHeaders { get; set; } = new[] 
        { 
            HeaderNames.ContentType, 
            Headers.TraceId,
            Headers.RetryAfter,
            Headers.RateLimitLimit,
            Headers.RateLimitReset,
            Headers.RateLimitPolicy,
            Headers.RateLimitWindow
        };

        /// <summary>
        /// Gets or sets whether credentials are allowed in CORS requests.
        /// </summary>
        public bool AllowCredentials { get; set; } = false;

        /// <summary>
        /// Gets or sets the preflight cache duration in minutes.
        /// </summary>
        public int PreflightMaxAgeMinutes { get; set; } = 60;

        /// <summary>
        /// Validates the CORS configuration.
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        public void Validate()
        {
            ValidatePolicyName();
            ValidateAllowedOrigins();
            ValidateAllowedMethods();
            ValidateAllowedHeaders();
            ValidateExposedHeaders();
            ValidatePreflightMaxAge();
        }

        private void ValidatePolicyName()
        {
            if (string.IsNullOrWhiteSpace(PolicyName))
                throw new ArgumentException("PolicyName cannot be null or empty", nameof(PolicyName));
        }

        private void ValidateAllowedOrigins()
        {
            if (AllowedOrigins == null)
                throw new ArgumentException("AllowedOrigins cannot be null", nameof(AllowedOrigins));

            if (AllowedOrigins.Length == 0)
                throw new ArgumentException("At least one allowed origin must be specified", nameof(AllowedOrigins));

            foreach (var origin in AllowedOrigins)
            {
                if (string.IsNullOrWhiteSpace(origin))
                    throw new ArgumentException("AllowedOrigins cannot contain null or empty values", nameof(AllowedOrigins));

                if (origin != "*" && !Uri.TryCreate(origin, UriKind.Absolute, out _))
                    throw new ArgumentException($"Invalid origin URL: '{origin}'. Must be a valid absolute URL or '*'", nameof(AllowedOrigins));
            }

            ValidateCredentialsWithWildcard();
        }

        private void ValidateCredentialsWithWildcard()
        {
            if (AllowCredentials && AllowedOrigins.Any(o => o == "*"))
                throw new ArgumentException(
                    "AllowCredentials cannot be true when AllowedOrigins contains '*'. Specify explicit origins instead.",
                    nameof(AllowCredentials));
        }

        private void ValidateAllowedMethods()
        {
            if (AllowedMethods == null || AllowedMethods.Length == 0)
                throw new ArgumentException("At least one allowed HTTP method must be specified", nameof(AllowedMethods));

            foreach (var method in AllowedMethods)
            {
                if (string.IsNullOrWhiteSpace(method))
                    throw new ArgumentException("AllowedMethods cannot contain null or empty values", nameof(AllowedMethods));
            }
        }

        private void ValidateAllowedHeaders()
        {
            if (AllowedHeaders == null)
                throw new ArgumentException("AllowedHeaders cannot be null", nameof(AllowedHeaders));
        }

        private void ValidateExposedHeaders()
        {
            if (ExposedHeaders == null)
                throw new ArgumentException("ExposedHeaders cannot be null", nameof(ExposedHeaders));
        }

        private void ValidatePreflightMaxAge()
        {
            if (PreflightMaxAgeMinutes <= 0 || PreflightMaxAgeMinutes > 86400) // Max 24 hours
                throw new ArgumentException("PreflightMaxAgeMinutes must be between 1 and 86400 (24 hours)", nameof(PreflightMaxAgeMinutes));
        }
    }
}
