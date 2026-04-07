namespace emc.camus.persistence.postgresql.Services;

/// <summary>
/// Tracks whether PostgreSQL repositories have completed their one-time startup initialization
/// (schema validation). Registered as a singleton so all scoped repository instances within the
/// same DI container share a single flag, while separate containers (e.g., multiple
/// <c>WebApplicationFactory</c> instances in integration tests) each get their own independent state.
/// </summary>
internal sealed class PSInitializationState
{
    /// <summary>
    /// Gets or sets whether the user repository has been initialized.
    /// </summary>
    public bool UserRepositoryInitialized { get; set; }

    /// <summary>
    /// Gets or sets whether the API info repository has been initialized.
    /// </summary>
    public bool ApiInfoRepositoryInitialized { get; set; }
}
