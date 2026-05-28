using System.Collections.Concurrent;
using emc.camus.application.Idempotency;
using emc.camus.cache.inmemory.Models;

namespace emc.camus.cache.inmemory.Services;

/// <summary>
/// In-memory implementation of <see cref="IIdempotencyResponseCache"/> using a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// Provides thread-safe idempotent response caching with TTL-based expiration.
/// Registered as a singleton — shared across all requests within the application lifetime.
/// </summary>
/// <remarks>
/// Suitable for single-instance deployments. For multi-instance or distributed scenarios,
/// replace with a Redis-backed implementation.
///
/// Cleanup strategy: expired entries are evicted lazily on retrieval (timestamp check).
/// </remarks>
internal sealed class IdempotencyResponseCache : IIdempotencyResponseCache
{
    private readonly ConcurrentDictionary<string, IdempotencyCacheEntry> _entries = new();
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a new instance of <see cref="IdempotencyResponseCache"/> with the specified time provider.
    /// </summary>
    /// <param name="timeProvider">The time provider used for TTL calculations.</param>
    public IdempotencyResponseCache(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Attempts to retrieve a cached response for the given composite key.
    /// Returns null if the entry does not exist or has expired.
    /// </summary>
    /// <param name="compositeKey">The composite cache key ({userId}:{idempotencyKey}).</param>
    /// <returns>The cached response if found and not expired; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="compositeKey"/> is null or whitespace.</exception>
    public CachedResponse? TryGet(string compositeKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(compositeKey);

        if (!_entries.TryGetValue(compositeKey, out var entry))
        {
            return null;
        }

        if (_timeProvider.GetUtcNow() >= entry.ExpiresAt)
        {
            _entries.TryRemove(compositeKey, out _);
            return null;
        }

        return entry.Response;
    }

    /// <summary>
    /// Stores a response in the cache with the specified TTL.
    /// </summary>
    /// <param name="compositeKey">The composite cache key ({userId}:{idempotencyKey}).</param>
    /// <param name="response">The response to cache.</param>
    /// <param name="ttl">The time-to-live for the cache entry.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="compositeKey"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="ttl"/> is zero or negative.</exception>
    public void Store(string compositeKey, CachedResponse response, TimeSpan ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(compositeKey);
        ArgumentNullException.ThrowIfNull(response);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);

        var entry = new IdempotencyCacheEntry(response, _timeProvider.GetUtcNow().Add(ttl));
        _entries[compositeKey] = entry;
    }
}
