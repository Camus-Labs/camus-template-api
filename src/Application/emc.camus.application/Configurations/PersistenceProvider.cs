namespace emc.camus.application.Configurations;

/// <summary>
/// Defines the supported persistence provider types.
/// </summary>
public enum PersistenceProvider
{
    /// <summary>
    /// In-memory persistence using configuration data.
    /// </summary>
    InMemory,

    /// <summary>
    /// PostgreSQL database persistence.
    /// </summary>
    PostgreSQL
}
