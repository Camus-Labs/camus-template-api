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
        public bool AllowCredentials { get; set; }

        /// <summary>
        /// Gets or sets the preflight cache duration in minutes.
        /// </summary>
        public int PreflightMaxAgeMinutes { get; set; } = DefaultPreflightMaxAgeMinutes;

        /// <summary>
        /// Validates the CORS configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
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
                throw new InvalidOperationException($"PolicyName cannot be null or empty. Got: '{PolicyName}'.");
            }

            if (PolicyName.Length > MaxPolicyNameLength)
            {
                throw new InvalidOperationException($"PolicyName must not exceed {MaxPolicyNameLength} characters. Current length: {PolicyName.Length}");
            }
        }

        private void ValidateAllowedOrigins()
        {
            if (AllowedOrigins == null)
                throw new InvalidOperationException($"AllowedOrigins cannot be null. Got: '{AllowedOrigins}'.");

            if (AllowedOrigins.Length == 0)
                throw new InvalidOperationException($"At least one allowed origin must be specified. Got: {AllowedOrigins.Length} origin(s).");

            foreach (var origin in AllowedOrigins)
            {
                if (string.IsNullOrWhiteSpace(origin))
                    throw new InvalidOperationException($"AllowedOrigins contains a null or empty value: '{origin}'.");

                if (origin != "*" && !Uri.TryCreate(origin, UriKind.Absolute, out _))
                    throw new InvalidOperationException($"Invalid origin URL: '{origin}'. Must be a valid absolute URL or '*'.");
            }
        }

        private void ValidateAllowCredentials()
        {
            if (AllowCredentials && AllowedOrigins.Any(o => o == "*"))
                throw new InvalidOperationException(
                    $"AllowCredentials cannot be true when AllowedOrigins contains '*'. Specify explicit origins instead.");
        }

        private void ValidateAllowedMethods()
        {
            if (AllowedMethods == null || AllowedMethods.Length == 0)
                throw new InvalidOperationException($"At least one allowed HTTP method must be specified. Got: {AllowedMethods?.Length ?? 0} method(s).");

            foreach (var method in AllowedMethods)
            {
                if (string.IsNullOrWhiteSpace(method))
                    throw new InvalidOperationException($"AllowedMethods contains a null or empty value: '{method}'.");
            }
        }

        private void ValidateAllowedHeaders()
        {
            if (AllowedHeaders == null)
                throw new InvalidOperationException($"AllowedHeaders cannot be null. Got: '{AllowedHeaders}'.");
        }

        private void ValidateExposedHeaders()
        {
            if (ExposedHeaders == null)
                throw new InvalidOperationException($"ExposedHeaders cannot be null. Got: '{ExposedHeaders}'.");
        }

        private void ValidatePreflightMaxAge()
        {
            if (PreflightMaxAgeMinutes < MinPreflightMaxAgeMinutes || PreflightMaxAgeMinutes > MaxPreflightMaxAgeMinutes)
            {
                throw new InvalidOperationException($"PreflightMaxAgeMinutes must be between {MinPreflightMaxAgeMinutes} and {MaxPreflightMaxAgeMinutes} (24 hours). Current value: {PreflightMaxAgeMinutes}");
            }
        }
    }
}
