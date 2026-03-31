using emc.camus.domain.Auth;

namespace emc.camus.application.ApiInfo;

/// <summary>
/// Repository contract for retrieving API information.
/// </summary>
public interface IApiInfoRepository
{
    /// <summary>
    /// Initializes the API info repository and validates the setup.
    /// </summary>
    /// <remarks>
    /// This method should be called at application startup to fail-fast if there are configuration issues.
    /// </remarks>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets API information by version.
    /// </summary>
    /// <param name="version">The API version to retrieve (e.g., "1.0", "2.0").</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>An ApiInfo object if found.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified version is not found.
    /// </exception>
    Task<domain.Auth.ApiInfo> GetByVersionAsync(string version, CancellationToken ct = default);
}
