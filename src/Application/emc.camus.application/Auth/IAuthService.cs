using emc.camus.application.Common;

namespace emc.camus.application.Auth;

/// <summary>
/// Defines the contract for the authentication application service.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates user credentials and generates a token.
    /// </summary>
    /// <param name="command">The authentication command containing username and password.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Authentication result with token, expiration, and user information.</returns>
    Task<AuthenticateUserResult> AuthenticateAsync(AuthenticateUserCommand command, CancellationToken ct = default);

    /// <summary>
    /// Generates a custom token with specified permissions and expiration for an authenticated user.
    /// </summary>
    /// <param name="command">The generate token command with suffix, expiration, and permissions.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Token generation result with token details and permissions.</returns>
    Task<GenerateTokenResult> GenerateTokenAsync(GenerateTokenCommand command, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paged list of generated tokens created by the currently authenticated user.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page number and page size).</param>
    /// <param name="filter">Optional filter criteria for the query.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A paged result of token summaries for the current user.</returns>
    Task<PagedResult<GeneratedTokenSummaryView>> GetGeneratedTokensAsync(PaginationParams pagination, GeneratedTokenFilter? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Revokes a generated token by its JTI. Only the creator of the token can revoke it.
    /// </summary>
    /// <param name="command">The revoke token command containing the JTI.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A summary view of the revoked token.</returns>
    Task<GeneratedTokenSummaryView> RevokeTokenAsync(RevokeTokenCommand command, CancellationToken ct = default);

    /// <summary>
    /// Initializes the user repository to load users and roles.
    /// Should be called during application startup.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken ct = default);
}
