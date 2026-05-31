namespace emc.camus.api.Configurations;

/// <summary>
/// Configuration settings for API rate limiting policies using a flat, fixed-keys layout.
/// Rate limiting is always enabled for security. Adjust limits per environment.
///
/// Implementation Details:
/// - Uses sliding window algorithm with configurable segments for smooth distribution
/// - IP-based rate limiting for ALL requests (runs before authentication)
/// - Returns 429 with RetryAfter header when limit exceeded
/// - No queueing (QueueLimit=0) - requests rejected immediately
/// - Three fixed policies: default, strict, relaxed (defined in <see cref="RateLimitPolicies"/>)
/// </summary>
public sealed class RateLimitingSettings
{
    /// <summary>
    /// The configuration section name for rate limit settings.
    /// </summary>
    public const string ConfigurationSectionName = "RateLimitingSettings";

    private const int MinSegmentsPerWindow = 1;
    private const int MaxSegmentsPerWindow = 20;
    private const int DefaultSegmentsPerWindowValue = 5;
    private const int MinPermitLimit = 1;
    private const int MaxPermitLimit = 100000;
    private const int MinWindowSeconds = 1;
    private const int MaxWindowSeconds = 3600;

    private static readonly string[] DefaultExemptPaths = new[] { "/health", "/ready", "/alive", "/swagger" };

    /// <summary>
    /// Number of segments per window for sliding window algorithm.
    /// Higher values provide smoother rate limiting but use more memory.
    /// Default: 5 segments (recommended: 3-10)
    /// </summary>
    public int SegmentsPerWindow { get; set; } = DefaultSegmentsPerWindowValue;

    /// <summary>
    /// Permit limit for the default rate limit policy.
    /// </summary>
    public int DefaultPermitLimit { get; set; } = 250;

    /// <summary>
    /// Window duration in seconds for the default rate limit policy.
    /// </summary>
    public int DefaultWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Permit limit for the strict rate limit policy (sensitive endpoints).
    /// </summary>
    public int StrictPermitLimit { get; set; } = 50;

    /// <summary>
    /// Window duration in seconds for the strict rate limit policy.
    /// </summary>
    public int StrictWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Permit limit for the relaxed rate limit policy (high-throughput endpoints).
    /// </summary>
    public int RelaxedPermitLimit { get; set; } = 500;

    /// <summary>
    /// Window duration in seconds for the relaxed rate limit policy.
    /// </summary>
    public int RelaxedWindowSeconds { get; set; } = 60;

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
        ValidateDefaultPermitLimit();
        ValidateStrictPermitLimit();
        ValidateRelaxedPermitLimit();
        ValidateDefaultWindowSeconds();
        ValidateStrictWindowSeconds();
        ValidateRelaxedWindowSeconds();
        ValidateExemptPaths();
    }

    private void ValidateSegmentsPerWindow()
    {
        if (SegmentsPerWindow < MinSegmentsPerWindow || SegmentsPerWindow > MaxSegmentsPerWindow)
        {
            throw new InvalidOperationException(
                $"SegmentsPerWindow must be between {MinSegmentsPerWindow} and {MaxSegmentsPerWindow}, but was {SegmentsPerWindow}.");
        }
    }

    private void ValidateDefaultPermitLimit()
    {
        if (DefaultPermitLimit < MinPermitLimit || DefaultPermitLimit > MaxPermitLimit)
        {
            throw new InvalidOperationException(
                $"DefaultPermitLimit must be between {MinPermitLimit} and {MaxPermitLimit}, but was {DefaultPermitLimit}.");
        }
    }

    private void ValidateStrictPermitLimit()
    {
        if (StrictPermitLimit < MinPermitLimit || StrictPermitLimit > MaxPermitLimit)
        {
            throw new InvalidOperationException(
                $"StrictPermitLimit must be between {MinPermitLimit} and {MaxPermitLimit}, but was {StrictPermitLimit}.");
        }
    }

    private void ValidateRelaxedPermitLimit()
    {
        if (RelaxedPermitLimit < MinPermitLimit || RelaxedPermitLimit > MaxPermitLimit)
        {
            throw new InvalidOperationException(
                $"RelaxedPermitLimit must be between {MinPermitLimit} and {MaxPermitLimit}, but was {RelaxedPermitLimit}.");
        }
    }

    private void ValidateDefaultWindowSeconds()
    {
        if (DefaultWindowSeconds < MinWindowSeconds || DefaultWindowSeconds > MaxWindowSeconds)
        {
            throw new InvalidOperationException(
                $"DefaultWindowSeconds must be between {MinWindowSeconds} and {MaxWindowSeconds}, but was {DefaultWindowSeconds}.");
        }
    }

    private void ValidateStrictWindowSeconds()
    {
        if (StrictWindowSeconds < MinWindowSeconds || StrictWindowSeconds > MaxWindowSeconds)
        {
            throw new InvalidOperationException(
                $"StrictWindowSeconds must be between {MinWindowSeconds} and {MaxWindowSeconds}, but was {StrictWindowSeconds}.");
        }
    }

    private void ValidateRelaxedWindowSeconds()
    {
        if (RelaxedWindowSeconds < MinWindowSeconds || RelaxedWindowSeconds > MaxWindowSeconds)
        {
            throw new InvalidOperationException(
                $"RelaxedWindowSeconds must be between {MinWindowSeconds} and {MaxWindowSeconds}, but was {RelaxedWindowSeconds}.");
        }
    }

    private void ValidateExemptPaths()
    {
        if (ExemptPaths is null)
        {
            throw new InvalidOperationException("ExemptPaths must not be null.");
        }

        for (var i = 0; i < ExemptPaths.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(ExemptPaths[i]))
            {
                throw new InvalidOperationException(
                    $"ExemptPaths contains a null or empty entry at index {i}.");
            }

            if (!ExemptPaths[i].StartsWith('/'))
            {
                throw new InvalidOperationException(
                    $"ExemptPaths entry '{ExemptPaths[i]}' must start with '/'.");
            }
        }
    }
}
