namespace emc.camus.application.Exceptions
{
    /// <summary>
    /// Exception thrown when a request exceeds the configured rate limit.
    /// Contains detailed information about the limit, policy, and retry timing.
    /// </summary>
    public class RateLimitExceededException : Exception
    {
        /// <summary>
        /// The name of the rate limit policy that was exceeded.
        /// </summary>
        public string PolicyName { get; }

        /// <summary>
        /// The maximum number of requests allowed in the time window.
        /// </summary>
        public int PermitLimit { get; }

        /// <summary>
        /// The time window in seconds for the rate limit.
        /// </summary>
        public int WindowSeconds { get; }

        /// <summary>
        /// The number of seconds the client should wait before retrying.
        /// </summary>
        public int RetryAfterSeconds { get; }

        /// <summary>
        /// The Unix timestamp when the rate limit window resets.
        /// </summary>
        public long ResetTimestamp { get; }

        /// <summary>
        /// Creates a new rate limit exceeded exception.
        /// </summary>
        /// <param name="policyName">The name of the policy that was exceeded.</param>
        /// <param name="permitLimit">The maximum number of requests allowed.</param>
        /// <param name="windowSeconds">The time window in seconds.</param>
        /// <param name="retryAfterSeconds">The number of seconds to wait before retrying.</param>
        /// <param name="resetTimestamp">The Unix timestamp when the limit resets.</param>
        public RateLimitExceededException(
            string policyName,
            int permitLimit,
            int windowSeconds,
            int retryAfterSeconds,
            long resetTimestamp)
            : base($"Rate limit exceeded for policy '{policyName}'. Limit: {permitLimit} requests per {windowSeconds} seconds. Retry after {retryAfterSeconds} seconds.")
        {
            PolicyName = policyName;
            PermitLimit = permitLimit;
            WindowSeconds = windowSeconds;
            RetryAfterSeconds = retryAfterSeconds;
            ResetTimestamp = resetTimestamp;
        }
    }
}
