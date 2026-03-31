using emc.camus.application.Common;

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
public class ApiInfoService : IApiInfoService
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
    /// <param name="filter">The filter containing the API version to retrieve</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>View containing API information for the requested version</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the requested version is not found in the repository</exception>
    /// <exception cref="InvalidOperationException">Thrown when database operations fail</exception>
    public virtual async Task<ApiInfoDetailView> GetByVersionAsync(ApiInfoFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        try
        {
            // Call repository to get domain entity (KeyNotFoundException bubbles up)
            var apiInfo = await _repository.GetByVersionAsync(filter.Version, ct);

            // Convert domain entity to application result
            return new ApiInfoDetailView(
                apiInfo.Version,
                apiInfo.Status,
                apiInfo.Features
            );
        }
        catch (Exception ex) when (ex is KeyNotFoundException or ArgumentException)
        {
            // Let domain exceptions bubble up with their original context
            throw;
        }
        catch (Exception ex)
        {
            // Wrap infrastructure failures with business context
            throw new InvalidOperationException(
                $"Failed to retrieve API information for version '{filter.Version}' due to a system error.", ex);
        }
    }

    /// <summary>
    /// Initializes the API info repository to load API data.
    /// Should be called during application startup.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when database connection fails or required tables don't exist.
    /// </exception>
    public virtual async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await _repository.InitializeAsync(ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to initialize API info service. Ensure the database is accessible.", ex);
        }
    }
}
