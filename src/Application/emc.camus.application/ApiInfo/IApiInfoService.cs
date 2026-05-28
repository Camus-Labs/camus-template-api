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
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>View containing API information for the requested version.</returns>
    Task<ApiInfoDetailView> GetByVersionAsync(ApiInfoFilter filter, CancellationToken ct = default);
}
