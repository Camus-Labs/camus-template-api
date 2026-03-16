using System.Diagnostics;
using System.Security.Claims;
using emc.camus.application.Auth;
using emc.camus.application.Common;

namespace emc.camus.api.Infrastructure;

/// <summary>
/// HTTP-based implementation of <see cref="IUserContext"/> that extracts user information
/// from the current HTTP request context.
/// </summary>
public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpUserContext"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Extracts the current user ID from the JWT <see cref="ClaimTypes.NameIdentifier"/> claim
    /// in the HTTP context. Returns <c>null</c> if the user is not authenticated or the claim is missing.
    /// </summary>
    /// <returns>The current user's ID, or <c>null</c> if unavailable.</returns>
    public Guid? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var subClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        return subClaim != null && Guid.TryParse(subClaim.Value, out var userId) ? userId : null;
    }

    /// <summary>
    /// Extracts the current username from the authenticated identity in the HTTP context.
    /// Returns <c>null</c> if the user is not authenticated.
    /// </summary>
    /// <returns>The current user's username, or <c>null</c> if unavailable.</returns>
    public string? GetCurrentUsername()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // If authenticated, Identity.Name should be set by the authentication handler
        // If it's null, that indicates a configuration problem that should be investigated
        return httpContext.User.Identity.Name;
    }

    /// <summary>
    /// Extracts the current user's permissions from claims of type <see cref="Permissions.ClaimType"/>
    /// in the HTTP context. Returns an empty list if the user is not authenticated.
    /// </summary>
    /// <returns>A list of permission strings for the current user.</returns>
    public List<string> GetCurrentPermissions()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return new List<string>();
        }

        return httpContext.User.Claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();
    }

    /// <summary>
    /// Retrieves the current distributed trace ID from <see cref="Activity.Current"/>.
    /// Returns <c>null</c> if no active trace exists.
    /// </summary>
    /// <returns>The current trace ID string, or <c>null</c> if unavailable.</returns>
    public string? GetCurrentTraceId()
    {
        return Activity.Current?.TraceId.ToString();
    }
}
