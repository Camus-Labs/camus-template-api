namespace emc.camus.application.Common;

/// <summary>
/// Constants for keyed service registration of database connection factories.
/// </summary>
public static class ConnectionFactoryKeys
{
    /// <summary>
    /// Key for application data connection factory (API info, etc.).
    /// </summary>
    public const string AppData = "AppData";

    /// <summary>
    /// Key for authorization connection factory (users, roles, permissions).
    /// </summary>
    public const string Authorization = "Authorization";
}
