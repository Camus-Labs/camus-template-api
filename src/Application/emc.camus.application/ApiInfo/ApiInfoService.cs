namespace emc.camus.application.ApiInfo;

/// <summary>
/// Application service for retrieving API information.
/// Orchestrates repository calls and converts domain objects to application views.
/// </summary>
/// <remarks>
/// This service follows the application layer pattern:
/// - Orchestrates calls to repositories
/// - Handles business logic validation
/// - Returns application view records (not domain entities)
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
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
    }

    /// <summary>
    /// Retrieves API information for a specific version.
    /// </summary>
    /// <param name="version">The API version to retrieve (e.g., "1.0", "2.0")</param>
    /// <returns>View containing API information for the requested version</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the requested version is not found in the repository</exception>
    /// <exception cref="ArgumentException">Thrown when version is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when database operations fail</exception>
    public virtual async Task<ApiInfoView> GetByVersionAsync(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        try
        {
            // Call repository to get domain entity (KeyNotFoundException bubbles up)
            var apiInfo = await _repository.GetByVersionAsync(version);

            // Convert domain entity to application result
            return new ApiInfoView(
                apiInfo.Version,
                apiInfo.Status,
                apiInfo.Features
            );
        }
        catch (Exception ex) when (ex is KeyNotFoundException)
        {
            // Let domain exceptions bubble up with their original context
            throw;
        }
        catch (Exception ex)
        {
            // Wrap infrastructure failures with business context
            throw new InvalidOperationException(
                $"Failed to retrieve API information for version '{version}' due to a system error.", ex);
        }
    }

    /// <summary>
    /// Retrieves all available API versions and their information.
    /// </summary>
    /// <returns>Collection of views containing API information for all versions</returns>
    /// <exception cref="InvalidOperationException">Thrown when database operations fail</exception>
    public virtual async Task<IEnumerable<ApiInfoView>> GetAllAsync()
    {
        try
        {
            // Call repository to get domain entities
            var apiInfos = await _repository.GetAllAsync();

            // Convert domain entities to application results
            return apiInfos.Select(apiInfo => new ApiInfoView(
                apiInfo.Version,
                apiInfo.Status,
                apiInfo.Features
            ));
        }
        catch (Exception ex)
        {
            // Wrap infrastructure failures with business context
            throw new InvalidOperationException(
                "Failed to retrieve API information due to a system error.", ex);
        }
    }

    /// <summary>
    /// Initializes the API info repository to load API data.
    /// Should be called during application startup.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when database connection fails or required tables don't exist.
    /// </exception>
    public virtual void Initialize()
    {
        try
        {
            _repository.Initialize();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to initialize API info service. Ensure the database is accessible.", ex);
        }
    }
}
