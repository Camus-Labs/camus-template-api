using System.Collections.Concurrent;
using emc.camus.application.Auth;

namespace emc.camus.cache.inmemory.Caches;

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
public class IMTokenRevocationCache : ITokenRevocationCache
{
    private readonly ConcurrentDictionary<Guid, DateTime> _revokedTokens = new();

    /// <inheritdoc />
    public bool IsRevoked(Guid jti)
    {
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

    /// <inheritdoc />
    public void Revoke(Guid jti, DateTime expiresOn)
    {
        if (expiresOn > DateTime.UtcNow)
        {
            _revokedTokens.TryAdd(jti, expiresOn);
        }
    }
}
