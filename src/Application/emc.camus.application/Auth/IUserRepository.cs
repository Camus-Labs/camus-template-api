using System.Data;
using emc.camus.application.Common;
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
    void Initialize();

    /// <summary>
    /// Validates user credentials and retrieves user information with roles using an external connection (for transactions).
    /// </summary>
    /// <param name="connection">The database connection to use for the operation.</param>
    /// <param name="username">The username to validate.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns>A User object with roles if credentials are valid.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when credentials are invalid.
    /// </exception>
    Task<User> ValidateCredentialsAsync(IDbConnection connection, string username, string password);

    /// <summary>
    /// Updates the last login timestamp for a user using an external connection (for transactions).
    /// </summary>
    /// <param name="connection">The database connection to use for the operation.</param>
    /// <param name="userId">The ID of the user to update.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task UpdateLastLoginAsync(IDbConnection connection, Guid userId);
}
