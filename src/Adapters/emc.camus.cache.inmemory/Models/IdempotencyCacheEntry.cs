using System.Diagnostics.CodeAnalysis;
using emc.camus.application.Idempotency;

namespace emc.camus.cache.inmemory.Models;

/// <summary>
/// Represents a cached idempotency response with its expiration timestamp.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed record IdempotencyCacheEntry(CachedResponse Response, DateTimeOffset ExpiresAt);
