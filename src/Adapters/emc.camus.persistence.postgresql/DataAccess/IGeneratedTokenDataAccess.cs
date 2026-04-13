using System.Data;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.DataAccess;

/// <summary>
/// Thin data access layer for generated token SQL operations.
/// </summary>
internal interface IGeneratedTokenDataAccess
{
    /// <summary>
    /// Checks whether a generated token with the given JTI exists.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="jti">The JTI to check.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>True if a token with the JTI exists; otherwise false.</returns>
    Task<bool> JtiExistsAsync(IDbConnection connection, Guid jti, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a generated token with the given token username exists.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="tokenUsername">The token username to check.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>True if a token with the username exists; otherwise false.</returns>
    Task<bool> TokenUsernameExistsAsync(IDbConnection connection, string tokenUsername, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a user with the given ID exists.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>True if the user exists; otherwise false.</returns>
    Task<bool> CreatorUserExistsAsync(IDbConnection connection, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Inserts a generated token record into the database.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="jti">The JTI.</param>
    /// <param name="creatorUserId">The creator user ID.</param>
    /// <param name="creatorUsername">The creator username.</param>
    /// <param name="tokenUsername">The token username.</param>
    /// <param name="permissions">The permissions array.</param>
    /// <param name="expiresOn">The expiration date.</param>
    /// <param name="isRevoked">Whether the token is revoked.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    Task InsertAsync(IDbConnection connection, Guid jti, Guid creatorUserId, string creatorUsername, string tokenUsername, string[] permissions, DateTime expiresOn, bool isRevoked, CancellationToken ct = default);

    /// <summary>
    /// Finds a generated token by its JTI.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="jti">The JTI to look up.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The generated token model if found; otherwise null.</returns>
    Task<GeneratedTokenModel?> FindByJtiAsync(IDbConnection connection, Guid jti, CancellationToken ct = default);

    /// <summary>
    /// Counts generated tokens for a creator user with optional filtering.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="creatorUserId">The creator user ID.</param>
    /// <param name="excludeRevoked">Whether to exclude revoked tokens.</param>
    /// <param name="excludeExpired">Whether to exclude expired tokens.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The total count of matching tokens.</returns>
    Task<int> CountByCreatorUserIdAsync(IDbConnection connection, Guid creatorUserId, bool excludeRevoked, bool excludeExpired, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a page of generated tokens for a creator user with optional filtering.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="creatorUserId">The creator user ID.</param>
    /// <param name="excludeRevoked">Whether to exclude revoked tokens.</param>
    /// <param name="excludeExpired">Whether to exclude expired tokens.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="offset">The offset for pagination.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The matching generated token models.</returns>
    Task<IEnumerable<GeneratedTokenModel>> GetPageByCreatorUserIdAsync(IDbConnection connection, Guid creatorUserId, bool excludeRevoked, bool excludeExpired, int pageSize, int offset, CancellationToken ct = default);

    /// <summary>
    /// Updates the revocation status of a generated token.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="jti">The JTI of the token to update.</param>
    /// <param name="isRevoked">The new revocation status.</param>
    /// <param name="revokedAt">The revocation timestamp.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> UpdateRevocationAsync(IDbConnection connection, Guid jti, bool isRevoked, DateTime? revokedAt, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all JTIs for revoked tokens that have not yet expired.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The JTIs of active revoked tokens.</returns>
    Task<IEnumerable<Guid>> GetActiveRevokedJtisAsync(IDbConnection connection, CancellationToken ct = default);
}
