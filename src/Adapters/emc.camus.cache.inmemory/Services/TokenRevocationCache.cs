using System.Collections.Concurrent;
using emc.camus.application.Auth;

namespace emc.camus.cache.inmemory.Services;

/// <summary>
/// In-memory implementation of <see cref="ITokenRevocationCache"/> using a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// Provides thread-safe token revocation checking.
/// Registered as a singleton — shared across all requests within the application lifetime.
/// </summary>
/// <remarks>
/// Suitable for single-instance deployments. For multi-instance or distributed scenarios,
/// replace with a Redis-backed implementation (emc.camus.cache.redis).
///
/// Cleanup strategy: expired entries are excluded by the repository query during periodic sync
/// by <see cref="TokenRevocationSyncService"/>, which replaces the full cache each cycle.
/// </remarks>
internal sealed class TokenRevocationCache : ITokenRevocationCache
{
    private readonly ConcurrentDictionary<Guid, byte> _revokedTokens = new();

    /// <summary>
    /// Checks whether a token identified by its JTI has been revoked.
    /// </summary>
    /// <param name="jti">The JWT ID to check.</param>
    /// <returns><see langword="true"/> if the token is revoked; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="jti"/> is <see cref="Guid.Empty"/>.</exception>
    public bool IsRevoked(Guid jti)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(jti, Guid.Empty);

        return _revokedTokens.ContainsKey(jti);
    }

    /// <summary>
    /// Records a token as revoked.
    /// </summary>
    /// <param name="jti">The JWT ID to revoke.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="jti"/> is <see cref="Guid.Empty"/>.</exception>
    public void Revoke(Guid jti)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(jti, Guid.Empty);

        _revokedTokens.TryAdd(jti, 0);
    }

    /// <summary>
    /// Replaces all cached entries with the provided revoked token set.
    /// Used by <see cref="TokenRevocationSyncService"/> to reload the cache from persistence.
    /// </summary>
    /// <param name="revokedJtis">The complete set of active revoked JTIs.</param>
    public void Refresh(HashSet<Guid> revokedJtis)
    {
        ArgumentNullException.ThrowIfNull(revokedJtis);

        _revokedTokens.Clear();
        foreach (var jti in revokedJtis)
        {
            _revokedTokens.TryAdd(jti, 0);
        }
    }
}
