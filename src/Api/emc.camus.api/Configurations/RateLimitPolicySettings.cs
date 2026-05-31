namespace emc.camus.api.Configurations;

/// <summary>
/// Represents a single rate limit policy with permit limit and time window.
/// </summary>
public sealed class RateLimitPolicySettings
{
    private const int MinPermitLimit = 1;
    private const int MaxPermitLimit = 100000;
    private const int MinWindowSeconds = 1;
    private const int MaxWindowSeconds = 3600;

    /// <summary>
    /// The name of this policy, used for identification in error messages and logging.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

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
    /// <exception cref="InvalidOperationException">Thrown when any setting is invalid.</exception>
    public void Validate()
    {
        ValidatePolicyName();
        ValidatePermitLimit();
        ValidateWindowSeconds();
    }

    private void ValidatePolicyName()
    {
        if (string.IsNullOrWhiteSpace(PolicyName))
        {
            throw new InvalidOperationException("PolicyName must be set before validation.");
        }
    }

    private void ValidatePermitLimit()
    {
        if (PermitLimit < MinPermitLimit || PermitLimit > MaxPermitLimit)
        {
            throw new InvalidOperationException(
                $"Policy '{PolicyName}': PermitLimit must be between {MinPermitLimit:N0} and {MaxPermitLimit:N0}. Current value: {PermitLimit}");
        }
    }

    private void ValidateWindowSeconds()
    {
        if (WindowSeconds < MinWindowSeconds || WindowSeconds > MaxWindowSeconds)
        {
            throw new InvalidOperationException(
                $"Policy '{PolicyName}': WindowSeconds must be between {MinWindowSeconds} and {MaxWindowSeconds:N0} (1 hour). Current value: {WindowSeconds}");
        }
    }
}
