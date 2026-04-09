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
    void Revoke(Guid jti);

    /// <summary>
    /// Replaces all cached entries with the provided set of revoked JTIs.
    /// Used by background synchronization to reload the cache from persistence.
    /// </summary>
    /// <param name="revokedJtis">The complete set of active revoked JTIs.</param>
    void Refresh(HashSet<Guid> revokedJtis);
}
