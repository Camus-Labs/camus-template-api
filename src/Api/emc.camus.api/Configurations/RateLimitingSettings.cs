namespace emc.camus.api.Configurations;

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
/// </summary>
public sealed class RateLimitingSettings
{
    /// <summary>
    /// The configuration section name for rate limit settings.
    /// </summary>
    public const string ConfigurationSectionName = "RateLimitingSettings";

    private const int MinSegmentsPerWindow = 1;
    private const int MaxSegmentsPerWindow = 20;
    private const int DefaultSegmentsPerWindow = 5;
    private const int DefaultPermitLimitDefault = 250;
    private const int DefaultPermitLimitStrict = 50;
    private const int DefaultPermitLimitRelaxed = 500;
    private const int DefaultWindowSeconds = 60;

    private static readonly string[] DefaultExemptPaths = new[] { "/health", "/ready", "/alive", "/swagger" };

    private static readonly Dictionary<string, RateLimitPolicySettings> DefaultPolicies = new()
    {
        { RateLimitPolicies.Default, new RateLimitPolicySettings { PermitLimit = DefaultPermitLimitDefault, WindowSeconds = DefaultWindowSeconds } },
        { RateLimitPolicies.Strict, new RateLimitPolicySettings { PermitLimit = DefaultPermitLimitStrict, WindowSeconds = DefaultWindowSeconds } },
        { RateLimitPolicies.Relaxed, new RateLimitPolicySettings { PermitLimit = DefaultPermitLimitRelaxed, WindowSeconds = DefaultWindowSeconds } }
    };

    /// <summary>
    /// Number of segments per window for sliding window algorithm.
    /// Higher values provide smoother rate limiting but use more memory.
    /// Default: 5 segments (recommended: 3-10)
    /// </summary>
    public int SegmentsPerWindow { get; set; } = DefaultSegmentsPerWindow;

    /// <summary>
    /// Named rate limit policies. Each policy defines permit limit and window.
    /// A "default" policy is required and will be used for endpoints without explicit policy.
    /// </summary>
    public Dictionary<string, RateLimitPolicySettings> Policies { get; set; } = DefaultPolicies;

    /// <summary>
    /// List of path prefixes that are exempt from rate limiting.
    /// </summary>
    public string[] ExemptPaths { get; set; } = DefaultExemptPaths;

    /// <summary>
    /// Validates the rate limit configuration at startup.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when any setting is invalid.</exception>
    public void Validate()
    {
        ValidateSegmentsPerWindow();
        ValidatePolicies();
        ValidateExemptPaths();
    }

    private void ValidateSegmentsPerWindow()
    {
        if (SegmentsPerWindow < MinSegmentsPerWindow || SegmentsPerWindow > MaxSegmentsPerWindow)
            throw new InvalidOperationException($"SegmentsPerWindow must be between {MinSegmentsPerWindow} and {MaxSegmentsPerWindow}. Current value: {SegmentsPerWindow}");
    }

    private void ValidatePolicies()
    {
        if (Policies == null || Policies.Count == 0)
            throw new InvalidOperationException("At least one rate limit policy must be defined");

        if (!Policies.ContainsKey(RateLimitPolicies.Default))
            throw new InvalidOperationException($"A '{RateLimitPolicies.Default}' rate limit policy must be defined");

        foreach (var (policyName, policy) in Policies)
        {
            ValidatePolicy(policyName, policy);
        }
    }

    private static void ValidatePolicy(string policyName, RateLimitPolicySettings policy)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            throw new InvalidOperationException("Policy name cannot be null or empty");

        var validPolicyNames = RateLimitPolicies.GetAll();
        if (!validPolicyNames.Contains(policyName))
        {
            var allowedNames = string.Join(", ", validPolicyNames);
            throw new InvalidOperationException(
                $"Invalid policy name '{policyName}'. Valid policy names are: {allowedNames} (case-sensitive)");
        }

        if (policy == null)
            throw new InvalidOperationException($"Policy '{policyName}' cannot be null");

        policy.PolicyName = policyName;
        policy.Validate();
    }

    private void ValidateExemptPaths()
    {
        if (ExemptPaths == null)
            throw new InvalidOperationException("ExemptPaths cannot be null");

        foreach (var path in ExemptPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("ExemptPaths cannot contain null or empty values");

            if (!path.StartsWith('/'))
                throw new InvalidOperationException($"ExemptPath '{path}' must start with '/' (e.g., '/health')");
        }
    }
}
