namespace emc.camus.cache.inmemory.Configurations;

/// <summary>
/// Configuration settings for the in-memory cache adapter.
/// Contains sub-sections for each cache type managed by this adapter.
/// </summary>
internal sealed class InMemoryCacheSettings
{
    /// <summary>
    /// The configuration section name used to bind these settings from appsettings.
    /// </summary>
    public const string ConfigurationSectionName = "InMemoryCacheSettings";

    /// <summary>
    /// Gets or sets the token revocation cache settings.
    /// </summary>
    public TokenRevocationCacheSettings TokenRevocationCache { get; set; } = new();

    /// <summary>
    /// Validates the configuration settings for all cache sub-sections.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when any setting is invalid.</exception>
    public void Validate()
    {
        ValidateTokenRevocationCache();
    }

    private void ValidateTokenRevocationCache()
    {
        TokenRevocationCache.Validate();
    }
}
