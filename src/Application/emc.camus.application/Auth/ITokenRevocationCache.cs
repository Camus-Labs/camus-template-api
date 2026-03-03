namespace emc.camus.application.Auth;

/// <summary>
/// Port for checking and managing token revocation state.
/// Provides fast in-memory lookups for revoked JTIs during JWT validation.
/// </summary>
public interface ITokenRevocationCache
{
    /// <summary>
    /// Checks whether a token with the given JTI has been revoked.
    /// </summary>
    /// <param name="jti">The JWT ID to check.</param>
    /// <returns>True if the token is revoked, false otherwise.</returns>
    bool IsRevoked(Guid jti);

    /// <summary>
    /// Marks a token as revoked in the cache.
    /// </summary>
    /// <param name="jti">The JWT ID to revoke.</param>
    /// <param name="expiresOn">The token's expiration date. Used for cache eviction — entries are removed after the token would have expired anyway.</param>
    void Revoke(Guid jti, DateTime expiresOn);
}
