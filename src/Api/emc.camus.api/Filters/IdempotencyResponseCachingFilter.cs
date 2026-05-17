using System.Security.Cryptography;
using emc.camus.api.Configurations;
using emc.camus.api.Metrics;
using emc.camus.application.Common;
using emc.camus.application.Exceptions;
using emc.camus.application.Idempotency;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace emc.camus.api.Filters;

/// <summary>
/// Resource filter that caches responses for idempotent POST requests.
/// On cache miss, executes the action, caches the response, and returns Idempotency-Key-Status: miss.
/// On cache hit with matching body hash, short-circuits with cached response and Idempotency-Key-Status: hit.
/// On body mismatch, returns HTTP 409 with error code idempotency_body_conflict.
/// </summary>
public partial class IdempotencyResponseCachingFilter : IAsyncResourceFilter
{
    private readonly IIdempotencyResponseCache _cache;
    private readonly IUserContext _userContext;
    private readonly IdempotencySettings _settings;
    private readonly IdempotencyMetrics _metrics;
    private readonly ILogger<IdempotencyResponseCachingFilter> _logger;

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Idempotency cache lookup failed for key {IdempotencyKey} \u2014 proceeding without caching")]
    private partial void LogCacheLookupFailed(Exception ex, string idempotencyKey);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Idempotency cache storage failed for key {IdempotencyKey}")]
    private partial void LogCacheStorageFailed(Exception ex, string idempotencyKey);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Idempotency-decorated endpoint returned {ResultType} instead of ObjectResult for key {IdempotencyKey} — response will not be cached")]
    private partial void LogUnexpectedResultType(string resultType, string idempotencyKey);

    /// <summary>
    /// Creates a new instance of IdempotencyResponseCachingFilter.
    /// </summary>
    /// <param name="cache">The idempotency response cache for storing and retrieving responses.</param>
    /// <param name="userContext">The user context for retrieving the authenticated principal.</param>
    /// <param name="settings">The idempotency TTL settings.</param>
    /// <param name="metrics">The idempotency metrics recorder.</param>
    /// <param name="logger">The logger for diagnostic messages.</param>
    public IdempotencyResponseCachingFilter(
        IIdempotencyResponseCache cache,
        IUserContext userContext,
        IdempotencySettings settings,
        IdempotencyMetrics metrics,
        ILogger<IdempotencyResponseCachingFilter> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(logger);

        _cache = cache;
        _userContext = userContext;
        _settings = settings;
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Executes the idempotency response caching logic around the resource execution pipeline.
    /// </summary>
    /// <param name="context">The resource executing context.</param>
    /// <param name="next">The delegate to execute the next filter or action.</param>
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var attribute = GetIdempotencyAttribute(context);
        if (attribute is null)
        {
            await next();
            return;
        }

        var userId = GetAuthenticatedUserId();

        var idempotencyKey = context.HttpContext.Request.Headers[Headers.IdempotencyKey].ToString();
        // Composite key ensures per-user isolation — different users cannot access each other's cached responses
        var cacheKey = $"{userId}:{idempotencyKey}";
        // Hash computed upfront to detect body mismatches without storing full request bodies
        var bodyHash = await ComputeBodyHashAsync(context.HttpContext.Request, context.HttpContext.RequestAborted);

        // Attempt to serve from cache — short-circuits on hit or body conflict
        var cached = TryGetCachedResponse(cacheKey, idempotencyKey);

        if (cached is not null)
        {
            ServeCachedResponse(context, cached, bodyHash);
            // Cache hit: replay the original response without re-executing the action
            _metrics.RecordCacheHit();
            context.HttpContext.Response.Headers[Headers.IdempotencyKeyStatus] = IdempotencyKeyStatuses.Hit;
            return;
        }

        // Set "miss" header before executing the action — at this point the response has not started,
        // so headers can be safely written. Once next() runs, the MVC pipeline may flush the response.
        context.HttpContext.Response.Headers[Headers.IdempotencyKeyStatus] = IdempotencyKeyStatuses.Miss;

        var executedContext = await next();

        // Never cache errors — allows clients to retry the same key on transient failures
        if (ShouldSkipCaching(executedContext))
        {
            return;
        }

        StoreResponse(cacheKey, bodyHash, executedContext, ResolveTtl(attribute.PolicyName), idempotencyKey);
    }

    /// <summary>
    /// Returns the idempotency attribute if the endpoint is decorated, otherwise null.
    /// </summary>
    private static RequireIdempotencyKeyAttribute? GetIdempotencyAttribute(ResourceExecutingContext context)
    {
        return context.ActionDescriptor.EndpointMetadata
            .OfType<RequireIdempotencyKeyAttribute>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Returns the authenticated user's ID. Throws if no user is authenticated — endpoints
    /// decorated with <see cref="RequireIdempotencyKeyAttribute"/> must require authentication.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no authenticated user is present on an idempotency-decorated endpoint.
    /// </exception>
    private Guid GetAuthenticatedUserId()
    {
        return _userContext.GetCurrentUserId()
            ?? throw new InvalidOperationException(
                $"Endpoint requires [{nameof(RequireIdempotencyKeyAttribute)}] but no authenticated user is present. " +
                "Ensure authentication middleware runs before the idempotency filter.");
    }

    /// <summary>
    /// Serves a cached response by short-circuiting the pipeline. Throws on body hash mismatch.
    /// </summary>
    /// <exception cref="DataConflictException">
    /// Thrown when the request body hash does not match the cached body hash.
    /// </exception>
    private void ServeCachedResponse(ResourceExecutingContext context, CachedResponse cached, string bodyHash)
    {
        // Body mismatch: same key reused with different payload — likely a client bug
        if (cached.BodyHash != bodyHash)
        {
            _metrics.RecordBodyConflict();
            throw new DataConflictException("Idempotency body conflict — request body hash does not match the cached request body.");
        }

        context.Result = new ObjectResult(cached.Body)
        {
            StatusCode = cached.StatusCode,
            ContentTypes = { "application/json" }
        };
    }

    /// <summary>
    /// Attempts to retrieve a cached response. Returns null if no entry exists or if the cache
    /// is unavailable (fail-open — logs warning and lets the request proceed without caching).
    /// </summary>
    private CachedResponse? TryGetCachedResponse(string cacheKey, string idempotencyKey)
    {
        try
        {
            return _cache.TryGet(cacheKey);
        }
        catch (Exception ex)
        {
            LogCacheLookupFailed(ex, idempotencyKey);
            _metrics.RecordCacheError();
            return null;
        }
    }

    /// <summary>
    /// Skips caching when the action threw an exception. Error-handling middleware runs
    /// after the filter pipeline, so at this point the only failure signal is an unhandled exception.
    /// </summary>
    private static bool ShouldSkipCaching(ResourceExecutedContext executedContext)
    {
        return executedContext.Exception is not null;
    }

    /// <summary>
    /// Persists the action result in the cache. Wrapped in try-catch to maintain
    /// fail-open behavior — a storage failure must not affect the client response.
    /// </summary>
    private void StoreResponse(
        string cacheKey,
        string bodyHash,
        ResourceExecutedContext executedContext,
        TimeSpan ttl,
        string idempotencyKey)
    {
        try
        {
            if (executedContext.Result is not ObjectResult objResult)
            {
                LogUnexpectedResultType(executedContext.Result?.GetType().Name ?? "null", idempotencyKey);
                return;
            }

            var statusCode = objResult.StatusCode ?? StatusCodes.Status200OK;
            var entry = new CachedResponse(statusCode, objResult.Value?.ToString(), bodyHash);
            _cache.Store(cacheKey, entry, ttl);
        }
        catch (Exception ex)
        {
            LogCacheStorageFailed(ex, idempotencyKey);
            _metrics.RecordCacheError();
        }
    }

    private static async Task<string> ComputeBodyHashAsync(HttpRequest request, CancellationToken ct = default)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        using var memoryStream = new MemoryStream();
        await request.Body.CopyToAsync(memoryStream, ct);
        request.Body.Position = 0;

        var hashBytes = SHA256.HashData(memoryStream.ToArray());
        return Convert.ToHexStringLower(hashBytes);
    }

    private TimeSpan ResolveTtl(string policyName)
    {
        return policyName switch
        {
            IdempotencyPolicies.LongTerm => TimeSpan.FromSeconds(_settings.LongTermTtlSeconds),
            _ => TimeSpan.FromSeconds(_settings.StandardTtlSeconds)
        };
    }
}
