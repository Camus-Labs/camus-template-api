namespace emc.camus.application.Configurations;

/// <summary>
/// Application data provider types.
/// </summary>
public enum AppDataProvider
{
    /// <summary>
    /// In-memory application data using configuration.
    /// </summary>
    InMemory,

    /// <summary>
    /// Database-backed application data.
    /// </summary>
    Database
}
