namespace emc.camus.ratelimiting.inmemory.Configurations
{
    /// <summary>
    /// Represents a single rate limit policy with permit limit and time window.
    /// Each policy defines how many requests are allowed within a specific time window.
    /// </summary>
    public class RateLimitPolicy
    {
        private const int MinPermitLimit = 1;
        private const int MaxPermitLimit = 100000;
        private const int MinWindowSeconds = 1;
        private const int MaxWindowSeconds = 3600;
        
        /// <summary>
        /// Number of requests allowed per window.
        /// </summary>
        public int PermitLimit { get; set; }

        /// <summary>
        /// Time window in seconds.
        /// </summary>
        public int WindowSeconds { get; set; }

        /// <summary>
        /// Validates the policy configuration.
        /// </summary>
        /// <param name="policyName">The name of the policy being validated (for error messages).</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate(string policyName)
        {
            ValidatePermitLimit(policyName);
            ValidateWindowSeconds(policyName);
        }

        private void ValidatePermitLimit(string policyName)
        {
            if (PermitLimit < MinPermitLimit || PermitLimit > MaxPermitLimit)
            {
                throw new InvalidOperationException(
                    $"Policy '{policyName}': PermitLimit must be between {MinPermitLimit:N0} and {MaxPermitLimit:N0}. Current value: {PermitLimit}");
            }
        }

        private void ValidateWindowSeconds(string policyName)
        {
            if (WindowSeconds < MinWindowSeconds || WindowSeconds > MaxWindowSeconds)
            {
                throw new InvalidOperationException(
                    $"Policy '{policyName}': WindowSeconds must be between {MinWindowSeconds} and {MaxWindowSeconds:N0} (1 hour). Current value: {WindowSeconds}");
            }
        }
    }
}
