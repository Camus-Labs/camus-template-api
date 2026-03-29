namespace emc.camus.application.ApiInfo;

/// <summary>
/// Defines the contract for the API information application service.
/// </summary>
public interface IApiInfoService
{
    /// <summary>
    /// Retrieves API information for a specific version.
    /// </summary>
    /// <param name="filter">The filter containing the API version to retrieve.</param>
    /// <returns>View containing API information for the requested version.</returns>
    Task<ApiInfoDetailView> GetByVersionAsync(ApiInfoFilter filter);

    /// <summary>
    /// Initializes the API info repository to load API data.
    /// Should be called during application startup.
    /// </summary>
    void Initialize();
}
