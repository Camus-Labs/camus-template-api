namespace emc.camus.ratelimiting.memory.Configurations
{
    /// <summary>
    /// Represents a single rate limit policy with permit limit and time window.
    /// Each policy defines how many requests are allowed within a specific time window.
    /// </summary>
    public class RateLimitPolicy
    {
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
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        /// <param name="policyName">The name of the policy being validated (for error messages).</param>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        public void Validate(string policyName)
        {
            if (PermitLimit <= 0 || PermitLimit > 100000)
            {
                throw new ArgumentException(
                    $"Policy '{policyName}': PermitLimit must be between 1 and 100,000. Current value: {PermitLimit}",
                    nameof(PermitLimit));
            }

            if (WindowSeconds <= 0 || WindowSeconds > 3600)
            {
                throw new ArgumentException(
                    $"Policy '{policyName}': WindowSeconds must be between 1 and 3,600 (1 hour). Current value: {WindowSeconds}",
                    nameof(WindowSeconds));
            }
        }
    }
}
