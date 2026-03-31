using emc.camus.domain.Auth;

namespace emc.camus.application.Auth;

/// <summary>
/// Repository contract for user credential validation and retrieval.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Initializes the user repository and validates the authentication setup.
    /// </summary>
    /// <remarks>
    /// This method should be called at application startup to fail-fast if there are configuration issues.
    /// </remarks>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Validates user credentials and retrieves user information with roles.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <param name="password">The password to validate.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A User object with roles if credentials are valid.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when credentials are invalid.
    /// </exception>
    Task<User> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The User with roles.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    Task<User> GetByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Updates the last login timestamp for a user.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task UpdateLastLoginAsync(Guid userId, CancellationToken ct = default);
}
