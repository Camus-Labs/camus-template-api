using Microsoft.Net.Http.Headers;

namespace emc.camus.api.Middleware;

/// <summary>
/// Adds security headers to all HTTP responses to mitigate common web vulnerabilities.
/// </summary>
/// <remarks>
/// This middleware sets the following headers:
/// <list type="bullet">
/// <item><c>X-Content-Type-Options: nosniff</c> — prevents MIME-sniffing attacks</item>
/// <item><c>X-Frame-Options: DENY</c> — prevents clickjacking by disallowing framing</item>
/// <item><c>Referrer-Policy: strict-origin-when-cross-origin</c> — limits referrer data leakage</item>
/// <item><c>X-XSS-Protection: 1; mode=block</c> — legacy XSS filter for older browsers and security scanners</item>
/// <item><c>Content-Security-Policy</c> — restricts resource loading sources to prevent XSS</item>
/// </list>
/// The Content-Security-Policy is relaxed for Swagger UI paths in Development to allow
/// inline scripts and styles required by the Swagger UI.
/// </remarks>
public sealed class SecurityHeadersMiddleware
{
    private const string StrictCsp =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';";

    private const string SwaggerCsp =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';";

    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="environment">The hosting environment.</param>
    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _isDevelopment = environment.IsDevelopment();
    }

    /// <summary>
    /// Processes the HTTP request and adds security headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevents browsers from MIME-sniffing the content type
            headers[HeaderNames.XContentTypeOptions] = "nosniff";

            // Prevents the page from being embedded in iframes (clickjacking protection)
            headers[HeaderNames.XFrameOptions] = "DENY";

            // Controls how much referrer information is sent with requests
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Legacy XSS filter — deprecated in modern browsers but still expected by security scanners
            headers[HeaderNames.XXSSProtection] = "1; mode=block";

            // Restricts which resources the browser is allowed to load (XSS/injection protection)
            var isSwagger = context.Request.Path.StartsWithSegments("/swagger");
            headers[HeaderNames.ContentSecurityPolicy] = _isDevelopment && isSwagger ? SwaggerCsp : StrictCsp;

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
