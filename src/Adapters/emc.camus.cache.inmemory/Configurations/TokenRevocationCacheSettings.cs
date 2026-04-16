namespace emc.camus.cache.inmemory.Configurations;

/// <summary>
/// Configuration settings for the token revocation cache background synchronization.
/// Controls whether and how frequently the cache is reloaded from persistence.
/// </summary>
internal sealed class TokenRevocationCacheSettings
{
    private const int MinSyncIntervalSeconds = 10;
    private const int MaxSyncIntervalSeconds = 86400;
    private const int DefaultSyncIntervalSeconds = 300;

    /// <summary>
    /// Gets or sets whether background synchronization with persistence is enabled.
    /// Defaults to <see langword="true"/>.
    /// Disable when no <c>IGeneratedTokenRepository</c> is registered (e.g., in-memory persistence mode).
    /// </summary>
    public bool SyncEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in seconds between cache synchronization cycles.
    /// Defaults to 300 seconds (5 minutes).
    /// </summary>
    public int SyncIntervalSeconds { get; set; } = DefaultSyncIntervalSeconds;

    /// <summary>
    /// Validates the token revocation cache settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when any setting is invalid.</exception>
    public void Validate()
    {
        ValidateSyncIntervalSeconds();
    }

    private void ValidateSyncIntervalSeconds()
    {
        if (SyncIntervalSeconds < MinSyncIntervalSeconds || SyncIntervalSeconds > MaxSyncIntervalSeconds)
            throw new InvalidOperationException(
                $"SyncIntervalSeconds must be between {MinSyncIntervalSeconds} and {MaxSyncIntervalSeconds}, but was {SyncIntervalSeconds}.");
    }
}
