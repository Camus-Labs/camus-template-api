using System.Diagnostics;
using System.Security.Claims;
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
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public string? GetCurrentTraceId()
    {
        return Activity.Current?.TraceId.ToString();
    }
}
