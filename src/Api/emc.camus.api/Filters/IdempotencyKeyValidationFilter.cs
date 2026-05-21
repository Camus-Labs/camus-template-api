using emc.camus.application.Common;
using Microsoft.AspNetCore.Mvc.Filters;

namespace emc.camus.api.Filters;

/// <summary>
/// Action filter that validates the presence and format of the Idempotency-Key header
/// on endpoints decorated with <see cref="RequireIdempotencyKeyAttribute"/>.
/// </summary>
public class IdempotencyKeyValidationFilter : IActionFilter
{
    private const int MaxKeyLength = 256;

    /// <summary>
    /// Validates the Idempotency-Key header before the action executes.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<RequireIdempotencyKeyAttribute>()
            .FirstOrDefault();

        if (attribute is null)
        {
            return;
        }

        var hasHeader = context.HttpContext.Request.Headers.TryGetValue(
            Headers.IdempotencyKey, out var headerValues);

        ValidateHeaderPresence(hasHeader, headerValues);
        ValidateHeaderValue(headerValues.ToString());
    }

    private static void ValidateHeaderPresence(bool hasHeader, Microsoft.Extensions.Primitives.StringValues headerValues)
    {
        if (!hasHeader || headerValues.Count == 0)
        {
            throw new ArgumentException("Idempotency-Key header is missing");
        }
    }

    private static void ValidateHeaderValue(string keyValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyValue, Headers.IdempotencyKey);

        if (keyValue.Length > MaxKeyLength)
        {
            throw new ArgumentException(
                $"Idempotency-Key header value exceeds maximum length (must be at most {MaxKeyLength} characters)");
        }
    }

    /// <summary>
    /// No-op after action execution.
    /// </summary>
    /// <param name="context">The action executed context.</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No post-execution logic required for validation
    }
}
