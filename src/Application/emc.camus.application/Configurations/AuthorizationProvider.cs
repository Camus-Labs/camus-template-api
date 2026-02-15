namespace emc.camus.application.Configurations;

/// <summary>
/// Authorization provider types.
/// </summary>
public enum AuthorizationProvider
{
    /// <summary>
    /// In-memory authorization using configuration.
    /// </summary>
    InMemory,

    /// <summary>
    /// Database-backed authorization.
    /// </summary>
    Database
}
