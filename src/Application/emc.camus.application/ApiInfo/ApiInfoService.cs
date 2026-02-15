namespace emc.camus.application.ApiInfo;

/// <summary>
/// Application service for retrieving API information.
/// Orchestrates repository calls and converts domain objects to application results.
/// </summary>
/// <remarks>
/// This service follows the application layer pattern:
/// - Orchestrates calls to repositories
/// - Handles business logic validation
/// - Returns application result records (not domain entities)
/// - Exceptions are handled by ExceptionHandlingMiddleware
/// </remarks>
public class ApiInfoService
{
    private readonly IApiInfoRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiInfoService"/> class.
    /// </summary>
    /// <param name="repository">Repository for retrieving API information.</param>
    public ApiInfoService(IApiInfoRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Retrieves API information for a specific version.
    /// </summary>
    /// <param name="version">The API version to retrieve (e.g., "1.0", "2.0")</param>
    /// <returns>Result containing API information for the requested version</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the requested version is not found in the repository</exception>
    /// <exception cref="ArgumentException">Thrown when version is null or empty</exception>
    public virtual async Task<ApiInfoResults> GetByVersionAsync(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be null or empty", nameof(version));

        // Call repository to get domain entity
        var apiInfo = await _repository.GetByVersionAsync(version);

        // Convert domain entity to application result
        return new ApiInfoResults(
            apiInfo.Version,
            apiInfo.Status,
            apiInfo.Features
        );
    }

    /// <summary>
    /// Retrieves all available API versions and their information.
    /// </summary>
    /// <returns>Collection of results containing API information for all versions</returns>
    public virtual async Task<IEnumerable<ApiInfoResults>> GetAllAsync()
    {
        // Call repository to get domain entities
        var apiInfos = await _repository.GetAllAsync();

        // Convert domain entities to application results
        return apiInfos.Select(apiInfo => new ApiInfoResults(
            apiInfo.Version,
            apiInfo.Status,
            apiInfo.Features
        ));
    }
}
