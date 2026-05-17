using System.Diagnostics.Metrics;
using emc.camus.application.Observability;

namespace emc.camus.api.Metrics;

/// <summary>
/// Provides metrics instrumentation for idempotency response caching operations.
/// Tracks cache hits, body conflicts, and cache error events.
/// </summary>
public sealed class IdempotencyMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _bodyConflictCounter;
    private readonly Counter<long> _cacheErrorCounter;

    /// <summary>
    /// Creates a new instance of IdempotencyMetrics with the specified service name.
    /// </summary>
    /// <param name="serviceName">The service name to use for the meter.</param>
    public IdempotencyMetrics(string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        _meter = new Meter($"{serviceName}{MeterNames.Infrastructure}");

        _cacheHitCounter = _meter.CreateCounter<long>(
            name: "idempotency_cache_hit_total",
            unit: "requests",
            description: "Total number of idempotency cache hits");

        _bodyConflictCounter = _meter.CreateCounter<long>(
            name: "idempotency_body_conflict_total",
            unit: "requests",
            description: "Total number of idempotency body conflict rejections");

        _cacheErrorCounter = _meter.CreateCounter<long>(
            name: "idempotency_cache_error_total",
            unit: "errors",
            description: "Total number of idempotency cache infrastructure errors");
    }

    /// <summary>
    /// Records a cache hit event.
    /// </summary>
    public void RecordCacheHit()
    {
        _cacheHitCounter.Add(1);
    }

    /// <summary>
    /// Records a body conflict event.
    /// </summary>
    public void RecordBodyConflict()
    {
        _bodyConflictCounter.Add(1);
    }

    /// <summary>
    /// Records a cache infrastructure error (lookup or storage failure).
    /// </summary>
    public void RecordCacheError()
    {
        _cacheErrorCounter.Add(1);
    }

    /// <summary>
    /// Disposes the meter.
    /// </summary>
    public void Dispose()
    {
        _meter.Dispose();
    }
}
