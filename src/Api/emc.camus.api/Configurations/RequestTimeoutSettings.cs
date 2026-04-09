namespace emc.camus.api.Configurations
{
    /// <summary>
    /// Configuration settings for request timeout policies.
    /// Allows overriding default timeout durations per policy via appsettings.json.
    ///
    /// Example Configuration:
    /// "RequestTimeoutSettings": {
    ///   "DefaultTimeoutSeconds": 30,
    ///   "TightTimeoutSeconds": 10,
    ///   "ExtendedTimeoutSeconds": 60
    /// }
    /// </summary>
    public sealed class RequestTimeoutSettings
    {
        /// <summary>
        /// Configuration section name in appsettings.json.
        /// </summary>
        public const string ConfigurationSectionName = "RequestTimeoutSettings";

        private const int MinTimeoutSeconds = 1;
        private const int MaxTimeoutSeconds = 300;

        /// <summary>
        /// Timeout in seconds for the default policy. Applies to all endpoints unless overridden.
        /// Default: 30 seconds.
        /// </summary>
        public int DefaultTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Timeout in seconds for the tight policy. Intended for fast endpoints (simple reads, auth).
        /// Default: 5 seconds.
        /// </summary>
        public int TightTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Timeout in seconds for the extended policy. Intended for slow endpoints (bulk, reports).
        /// Default: 60 seconds.
        /// </summary>
        public int ExtendedTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Validates the request timeout configuration at startup.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateDefaultTimeoutSeconds();
            ValidateTightTimeoutSeconds();
            ValidateExtendedTimeoutSeconds();
        }

        private void ValidateDefaultTimeoutSeconds()
        {
            if (DefaultTimeoutSeconds < MinTimeoutSeconds || DefaultTimeoutSeconds > MaxTimeoutSeconds)
                throw new InvalidOperationException(
                    $"DefaultTimeoutSeconds must be between {MinTimeoutSeconds} and {MaxTimeoutSeconds}, but was {DefaultTimeoutSeconds}");
        }

        private void ValidateTightTimeoutSeconds()
        {
            if (TightTimeoutSeconds < MinTimeoutSeconds || TightTimeoutSeconds > MaxTimeoutSeconds)
                throw new InvalidOperationException(
                    $"TightTimeoutSeconds must be between {MinTimeoutSeconds} and {MaxTimeoutSeconds}, but was {TightTimeoutSeconds}");
        }

        private void ValidateExtendedTimeoutSeconds()
        {
            if (ExtendedTimeoutSeconds < MinTimeoutSeconds || ExtendedTimeoutSeconds > MaxTimeoutSeconds)
                throw new InvalidOperationException(
                    $"ExtendedTimeoutSeconds must be between {MinTimeoutSeconds} and {MaxTimeoutSeconds}, but was {ExtendedTimeoutSeconds}");
        }
    }
}
