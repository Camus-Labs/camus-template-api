namespace emc.camus.application.Idempotency;

/// <summary>
/// Port for storing and retrieving cached idempotent responses.
/// Implementations provide in-process caching with TTL-based expiration,
/// keyed by a composite of authenticated principal and idempotency key.
/// </summary>
public interface IIdempotencyResponseCache
{
    /// <summary>
    /// Attempts to retrieve a cached response for the given composite key.
    /// </summary>
    /// <param name="compositeKey">The composite cache key ({userId}:{idempotencyKey}).</param>
    /// <returns>The cached response if found and not expired; otherwise <see langword="null"/>.</returns>
    CachedResponse? TryGet(string compositeKey);

    /// <summary>
    /// Stores a response in the cache with the specified TTL.
    /// </summary>
    /// <param name="compositeKey">The composite cache key ({userId}:{idempotencyKey}).</param>
    /// <param name="response">The response to cache.</param>
    /// <param name="ttl">The time-to-live for the cache entry.</param>
    void Store(string compositeKey, CachedResponse response, TimeSpan ttl);
}
