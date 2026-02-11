using emc.camus.application.RateLimiting;

namespace emc.camus.ratelimiting.memory.Configurations
{
    /// <summary>
    /// Configuration settings for API rate limiting policies.
    /// Rate limiting is always enabled for security. Adjust limits per environment.
    /// 
    /// Implementation Details:
    /// - Uses sliding window algorithm with configurable segments for smooth distribution
    /// - IP-based rate limiting for ALL requests (runs before authentication)
    /// - Returns 429 with RetryAfter header when limit exceeded
    /// - No queueing (QueueLimit=0) - requests rejected immediately
    /// - Policy-based approach allows different limits per endpoint
    /// 
    /// Security Note:
    /// Rate limiting runs BEFORE authentication middleware to protect auth endpoints.
    /// This means we cannot distinguish between authenticated and anonymous users.
    /// All requests from the same IP address share the same rate limit for a given policy.
    /// 
    /// Example Configuration:
    /// "SegmentsPerWindow": 5,
    /// "Policies": {
    ///   "default": { "PermitLimit": 100, "WindowSeconds": 60 },
    ///   "strict": { "PermitLimit": 10, "WindowSeconds": 60 },
    ///   "relaxed": { "PermitLimit": 1000, "WindowSeconds": 60 }
    /// }
    /// </summary>
    public class RateLimitSettings
    {
        /// <summary>
        /// The configuration section name for rate limit settings.
        /// </summary>
        public const string ConfigurationSectionName = "RateLimitSettings";
        
        /// <summary>
        /// Number of segments per window for sliding window algorithm.
        /// Higher values provide smoother rate limiting but use more memory.
        /// This is a global setting applied to all policies.
        /// Default: 5 segments (recommended: 3-10)
        /// </summary>
        public int SegmentsPerWindow { get; set; } = 5;

        /// <summary>
        /// Named rate limit policies. Each policy defines permit limit and window.
        /// Endpoints can specify which policy to use via [RateLimit(RateLimitPolicies.PolicyName)] attribute.
        /// A "default" policy is required and will be used for endpoints without explicit policy.
        /// </summary>
        public Dictionary<string, RateLimitPolicy> Policies { get; set; } = new()
        {
            { RateLimitPolicies.Default, new RateLimitPolicy { PermitLimit = 250, WindowSeconds = 60 } },
            { RateLimitPolicies.Strict, new RateLimitPolicy { PermitLimit = 50, WindowSeconds = 60 } },
            { RateLimitPolicies.Relaxed, new RateLimitPolicy { PermitLimit = 500, WindowSeconds = 60 } }
        };

        /// <summary>
        /// List of path prefixes that are exempt from rate limiting (e.g., health checks, metrics).
        /// Paths are case-insensitive and matched using StartsWith.
        /// </summary>
        public string[] ExemptPaths { get; set; } = new[] { "/health", "/ready", "/alive", "/swagger" };

        /// <summary>
        /// Validates the rate limit configuration at startup.
        /// Throws ArgumentException if any setting is invalid to enable fail-fast deployment.
        /// 
        /// Validation Rules:
        /// - SegmentsPerWindow: 1-20 (balances memory usage vs smoothness)
        /// - Policies: Must contain at least "default" policy
        /// - PermitLimit: 1-100,000 requests per policy
        /// - WindowSeconds: 1-3,600 seconds (1 hour max) per policy
        /// - ExemptPaths: Must start with '/' and not be empty/whitespace
        /// 
        /// Note: Validation logic is kept in this class for simplicity rather than
        /// a separate validator, as configuration validation is part of the adapter's
        /// responsibility and not shared business logic.
        /// </summary>
        public void Validate()
        {
            ValidateSegmentsPerWindow();
            ValidatePolicies();
            ValidateExemptPaths();
        }

        private void ValidateSegmentsPerWindow()
        {
            if (SegmentsPerWindow <= 0 || SegmentsPerWindow > 20)
                throw new ArgumentException("SegmentsPerWindow must be between 1 and 20", nameof(SegmentsPerWindow));
        }

        private void ValidatePolicies()
        {
            if (Policies == null || Policies.Count == 0)
                throw new ArgumentException("At least one rate limit policy must be defined", nameof(Policies));
            
            if (!Policies.ContainsKey(RateLimitPolicies.Default))
                throw new ArgumentException($"A '{RateLimitPolicies.Default}' rate limit policy must be defined", nameof(Policies));
            
            foreach (var (policyName, policy) in Policies)
            {
                ValidatePolicy(policyName, policy);
            }
        }

        private void ValidatePolicy(string policyName, RateLimitPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(policyName))
                throw new ArgumentException("Policy name cannot be null or empty", nameof(Policies));
            
            if (policy == null)
                throw new ArgumentException($"Policy '{policyName}' cannot be null", nameof(Policies));
            
            policy.Validate(policyName);
        }

        private void ValidateExemptPaths()
        {
            if (ExemptPaths == null)
                throw new ArgumentException("ExemptPaths cannot be null", nameof(ExemptPaths));
            
            foreach (var path in ExemptPaths)
            {
                ValidateExemptPath(path);
            }
        }

        private void ValidateExemptPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("ExemptPaths cannot contain null or empty values", nameof(ExemptPaths));
            
            if (!path.StartsWith('/'))
                throw new ArgumentException($"ExemptPath '{path}' must start with '/' (e.g., '/health')", nameof(ExemptPaths));
        }
    }
}
