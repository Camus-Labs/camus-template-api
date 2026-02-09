namespace emc.camus.application.RateLimiting
{
    /// <summary>
    /// Specifies which rate limit policy to apply to a controller or action.
    /// The policy name must match a policy defined in RateLimitSettings.Policies configuration.
    /// If no attribute is present, the "default" policy is used.
    /// </summary>
    /// <example>
    /// [RateLimit(RateLimitPolicies.Strict)] - Apply "strict" policy (e.g., 10 req/min)
    /// [RateLimit(RateLimitPolicies.Relaxed)] - Apply "relaxed" policy (e.g., 1000 req/min)
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RateLimitAttribute : Attribute
    {
        /// <summary>
        /// The name of the rate limit policy to apply.
        /// Must match a policy defined in configuration.
        /// </summary>
        public string PolicyName { get; }

        /// <summary>
        /// Creates a new rate limit attribute with the specified policy name.
        /// </summary>
        /// <param name="policyName">The name of the rate limit policy to apply.</param>
        /// <exception cref="ArgumentException">Thrown if policy name is null or whitespace.</exception>
        public RateLimitAttribute(string policyName)
        {
            if (string.IsNullOrWhiteSpace(policyName))
                throw new ArgumentException("Policy name cannot be null or whitespace", nameof(policyName));
            
            PolicyName = policyName;
        }
    }
}
