using System.Collections.Concurrent;
using emc.camus.application.Auth;

namespace emc.camus.cache.inmemory.Services;

/// <summary>
/// In-memory implementation of <see cref="ITokenRevocationCache"/> using a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// Provides thread-safe token revocation checking with lazy eviction of expired entries.
/// Registered as a singleton — shared across all requests within the application lifetime.
/// </summary>
/// <remarks>
/// Suitable for single-instance deployments. For multi-instance or distributed scenarios,
/// replace with a Redis-backed implementation (emc.camus.cache.redis).
/// 
/// Eviction strategy: expired tokens are removed lazily during <see cref="IsRevoked"/> lookups.
/// No background cleanup thread is used, keeping the implementation simple and predictable.
/// </remarks>
internal sealed class IMTokenRevocationCache : ITokenRevocationCache
{
    private readonly ConcurrentDictionary<Guid, DateTime> _revokedTokens = new();

    /// <summary>
    /// Checks whether a token identified by its JTI has been revoked.
    /// Lazily evicts expired entries encountered during lookup.
    /// </summary>
    /// <param name="jti">The JWT ID to check.</param>
    /// <returns><see langword="true"/> if the token is revoked and not yet expired; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="jti"/> is <see cref="Guid.Empty"/>.</exception>
    public bool IsRevoked(Guid jti)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(jti, Guid.Empty);

        if (_revokedTokens.TryGetValue(jti, out var expiresOn))
        {
            if (expiresOn <= DateTime.UtcNow)
            {
                _revokedTokens.TryRemove(jti, out _);
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Records a token as revoked until its expiration time.
    /// Ignores tokens that have already expired.
    /// </summary>
    /// <param name="jti">The JWT ID to revoke.</param>
    /// <param name="expiresOn">The token's expiration date, used for cache eviction.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="jti"/> is <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="expiresOn"/> is <see langword="default"/>.</exception>
    public void Revoke(Guid jti, DateTime expiresOn)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(jti, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(expiresOn, default);

        if (expiresOn > DateTime.UtcNow)
        {
            _revokedTokens.TryAdd(jti, expiresOn);
        }
    }
}
