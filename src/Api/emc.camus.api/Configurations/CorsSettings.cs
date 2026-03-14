using emc.camus.application.Common;
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

        private const string DefaultPolicyName = "DefaultCorsPolicy";
        private const int DefaultPreflightMaxAgeMinutes = 60;
        private const int MaxPolicyNameLength = 100;
        private const int MinPreflightMaxAgeMinutes = 1;
        private const int MaxPreflightMaxAgeMinutes = 86400; // 24 hours
        private static readonly string[] DefaultAllowedMethods = new[] { "GET", "POST" };

        /// <summary>
        /// Gets or sets the name of the CORS policy.
        /// </summary>
        public string PolicyName { get; set; } = DefaultPolicyName;

        /// <summary>
        /// Gets or sets the allowed origins for CORS requests.
        /// </summary>
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the allowed HTTP methods for CORS requests.
        /// </summary>
        public string[] AllowedMethods { get; set; } = DefaultAllowedMethods;

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
        public int PreflightMaxAgeMinutes { get; set; } = DefaultPreflightMaxAgeMinutes;

        /// <summary>
        /// Validates the CORS configuration.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidatePolicyName();
            ValidateAllowedOrigins();
            ValidateAllowedMethods();
            ValidateAllowedHeaders();
            ValidateExposedHeaders();
            ValidatePreflightMaxAge();
            ValidateAllowCredentials();
        }

        private void ValidatePolicyName()
        {
            if (string.IsNullOrWhiteSpace(PolicyName))
            {
                throw new ArgumentException($"PolicyName cannot be null or empty. Got: '{PolicyName}'.", nameof(PolicyName));
            }

            if (PolicyName.Length > MaxPolicyNameLength)
            {
                throw new ArgumentException($"PolicyName must not exceed {MaxPolicyNameLength} characters. Current length: {PolicyName.Length}", nameof(PolicyName));
            }
        }

        private void ValidateAllowedOrigins()
        {
            if (AllowedOrigins == null)
                throw new ArgumentException($"AllowedOrigins cannot be null. Got: '{AllowedOrigins}'.", nameof(AllowedOrigins));

            if (AllowedOrigins.Length == 0)
                throw new ArgumentException($"At least one allowed origin must be specified. Got: {AllowedOrigins.Length} origin(s).", nameof(AllowedOrigins));

            foreach (var origin in AllowedOrigins)
            {
                if (string.IsNullOrWhiteSpace(origin))
                    throw new ArgumentException($"AllowedOrigins contains a null or empty value: '{origin}'.", nameof(AllowedOrigins));

                if (origin != "*" && !Uri.TryCreate(origin, UriKind.Absolute, out _))
                    throw new ArgumentException($"Invalid origin URL: '{origin}'. Must be a valid absolute URL or '*'.", nameof(AllowedOrigins));
            }
        }

        private void ValidateAllowCredentials()
        {
            if (AllowCredentials && AllowedOrigins.Any(o => o == "*"))
                throw new ArgumentException(
                    $"AllowCredentials cannot be true when AllowedOrigins contains '*'. Specify explicit origins instead.",
                    nameof(AllowCredentials));
        }

        private void ValidateAllowedMethods()
        {
            if (AllowedMethods == null || AllowedMethods.Length == 0)
                throw new ArgumentException($"At least one allowed HTTP method must be specified. Got: {AllowedMethods?.Length ?? 0} method(s).", nameof(AllowedMethods));

            foreach (var method in AllowedMethods)
            {
                if (string.IsNullOrWhiteSpace(method))
                    throw new ArgumentException($"AllowedMethods contains a null or empty value: '{method}'.", nameof(AllowedMethods));
            }
        }

        private void ValidateAllowedHeaders()
        {
            if (AllowedHeaders == null)
                throw new ArgumentException($"AllowedHeaders cannot be null. Got: '{AllowedHeaders}'.", nameof(AllowedHeaders));
        }

        private void ValidateExposedHeaders()
        {
            if (ExposedHeaders == null)
                throw new ArgumentException($"ExposedHeaders cannot be null. Got: '{ExposedHeaders}'.", nameof(ExposedHeaders));
        }

        private void ValidatePreflightMaxAge()
        {
            if (PreflightMaxAgeMinutes < MinPreflightMaxAgeMinutes || PreflightMaxAgeMinutes > MaxPreflightMaxAgeMinutes)
            {
                throw new ArgumentException($"PreflightMaxAgeMinutes must be between {MinPreflightMaxAgeMinutes} and {MaxPreflightMaxAgeMinutes} (24 hours). Current value: {PreflightMaxAgeMinutes}", nameof(PreflightMaxAgeMinutes));
            }
        }
    }
}
