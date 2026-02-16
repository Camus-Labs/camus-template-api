namespace emc.camus.application.Common;

/// <summary>
/// Flags to control which persistence features are registered.
/// </summary>
[Flags]
public enum PersistenceFeatures
{
    /// <summary>No features registered.</summary>
    None = 0,
    
    /// <summary>Register authentication/authorization repositories (IUserRepository).</summary>
    Auth = 1,
    
    /// <summary>Register application data repositories (IApiInfoRepository).</summary>
    AppData = 2,
    
    /// <summary>Register all features.</summary>
    All = Auth | AppData
}
