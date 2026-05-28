namespace emc.camus.api.Configurations;

/// <summary>
/// Configuration settings for idempotency TTL policies.
/// Allows overriding default TTL durations per policy via appsettings.json.
///
/// Example Configuration:
/// "IdempotencySettings": {
///   "StandardTtlSeconds": 300,
///   "LongTermTtlSeconds": 86400
/// }
/// </summary>
public sealed class IdempotencySettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string ConfigurationSectionName = "IdempotencySettings";

    private const int MinTtlSeconds = 1;
    private const int MaxTtlSeconds = 604800;
    private const int DefaultStandardTtlSeconds = 300;
    private const int DefaultLongTermTtlSeconds = 86400;

    /// <summary>
    /// TTL in seconds for the standard idempotency policy.
    /// Default: 300 seconds (5 minutes).
    /// </summary>
    public int StandardTtlSeconds { get; set; } = DefaultStandardTtlSeconds;

    /// <summary>
    /// TTL in seconds for the long-term idempotency policy.
    /// Default: 86400 seconds (24 hours).
    /// </summary>
    public int LongTermTtlSeconds { get; set; } = DefaultLongTermTtlSeconds;

    /// <summary>
    /// Validates the idempotency configuration at startup.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateStandardTtlSeconds();
        ValidateLongTermTtlSeconds();
    }

    private void ValidateStandardTtlSeconds()
    {
        if (StandardTtlSeconds < MinTtlSeconds || StandardTtlSeconds > MaxTtlSeconds)
            throw new InvalidOperationException(
                $"StandardTtlSeconds must be between {MinTtlSeconds} and {MaxTtlSeconds}, but was {StandardTtlSeconds}.");
    }

    private void ValidateLongTermTtlSeconds()
    {
        if (LongTermTtlSeconds < MinTtlSeconds || LongTermTtlSeconds > MaxTtlSeconds)
            throw new InvalidOperationException(
                $"LongTermTtlSeconds must be between {MinTtlSeconds} and {MaxTtlSeconds}, but was {LongTermTtlSeconds}.");
    }
}
