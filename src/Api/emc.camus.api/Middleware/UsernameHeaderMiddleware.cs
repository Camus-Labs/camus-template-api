using emc.camus.application.Common;
using Microsoft.AspNetCore.Http;

namespace emc.camus.api.Middleware;

/// <summary>
/// Adds Username header to all HTTP responses.
/// </summary>
/// <remarks>
/// This middleware automatically includes the user's identity in response headers:
/// <list type="bullet">
/// <item>For authenticated requests: includes the username from JWT claims</item>
/// <item>For anonymous requests: includes "anonymous" as the user identifier</item>
/// </list>
/// This is useful for:
/// <list type="bullet">
/// <item>Client-side debugging and logging</item>
/// <item>Correlating requests with specific users or anonymous traffic</item>
/// <item>Audit trails and analytics</item>
/// <item>Consistent observability across all requests</item>
/// </list>
/// </remarks>
public sealed class UsernameHeaderMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public UsernameHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and adds Username header to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Capture username early to avoid issues with null Identity
        // Use "anonymous" for unauthenticated requests to maintain consistent observability
        var username = context.User?.Identity?.IsAuthenticated == true
            ? context.User.Identity.Name
            : "anonymous";

        context.Response.OnStarting(() =>
        {
            if (!string.IsNullOrWhiteSpace(username) && !context.Response.Headers.ContainsKey(Headers.Username))
            {
                context.Response.Headers[Headers.Username] = username;
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
