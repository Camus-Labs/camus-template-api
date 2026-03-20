namespace emc.camus.application.Configurations;

/// <summary>
/// Configuration settings for data persistence provider selection.
/// Determines whether the application uses in-memory or PostgreSQL persistence.
/// Provider-specific settings are configured in their own top-level sections
/// (<see cref="DatabaseSettings"/> or the adapter's InMemoryModelSettings).
/// </summary>
public class DataPersistenceSettings
{
    /// <summary>
    /// Gets the configuration section name for data persistence settings.
    /// </summary>
    public const string ConfigurationSectionName = "DataPersistenceSettings";

    /// <summary>
    /// Gets or sets the persistence provider type.
    /// </summary>
    public PersistenceProvider Provider { get; set; } = PersistenceProvider.InMemory;

    /// <summary>
    /// Validates the data persistence settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any setting is invalid.
    /// </exception>
    public void Validate()
    {
        ValidateProvider();
    }

    private void ValidateProvider()
    {
        if (!Enum.IsDefined(Provider))
        {
            throw new InvalidOperationException($"Invalid persistence provider: {Provider}. Must be 'InMemory' or 'PostgreSQL'.");
        }
    }
}
