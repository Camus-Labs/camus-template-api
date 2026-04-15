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
        context.Response.OnStarting(AddUsernameHeader, context.Response);
        await _next(context);
    }

    private static Task AddUsernameHeader(object state)
    {
        var response = (HttpResponse)state;
        var identity = response.HttpContext.User.Identity;
        var username = identity != null && identity.IsAuthenticated && identity.Name != null
            ? identity.Name
            : "anonymous";

        response.Headers[Headers.Username] = username;
        return Task.CompletedTask;
    }
}
