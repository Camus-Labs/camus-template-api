using emc.camus.application.Common;
using emc.camus.domain.Auth;

namespace emc.camus.application.Auth;

/// <summary>
/// Provides functionality for managing generated tokens with custom permissions and expiration.
/// </summary>
public interface IGeneratedTokenRepository
{
    /// <summary>
    /// Creates a new generated token record in the database.
    /// Entity-centric: accepts the domain entity, repository owns lifecycle defaults (created_at).
    /// </summary>
    /// <param name="generatedToken">The generated token domain entity.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(GeneratedToken generatedToken, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a generated token by its JTI (JWT ID).
    /// </summary>
    /// <param name="jti">The JWT ID to search for.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The generated token if found, otherwise null.</returns>
    Task<GeneratedToken?> GetByJtiAsync(Guid jti, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paged list of generated tokens for a specific creator user.
    /// </summary>
    /// <param name="creatorUserId">The user ID of the creator.</param>
    /// <param name="pagination">Pagination parameters (page number and page size).</param>
    /// <param name="filter">Optional filter criteria for the query.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A paged result containing the tokens and pagination metadata.</returns>
    Task<PagedResult<GeneratedToken>> GetPagedByCreatorUserIdAsync(Guid creatorUserId, PaginationParams pagination, GeneratedTokenFilter? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Persists the current state of a generated token (e.g., after Revoke()).
    /// Entity-centric: accepts the mutated domain entity, repository owns lifecycle updates (updated_at).
    /// </summary>
    /// <param name="generatedToken">The generated token domain entity with updated state.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(GeneratedToken generatedToken, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all revoked tokens that have not yet expired.
    /// Used by background cache synchronization to reload the revocation denylist.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A set of JTIs for revoked tokens that have not yet expired.</returns>
    Task<HashSet<Guid>> GetActiveRevokedJtisAsync(CancellationToken ct = default);
}
