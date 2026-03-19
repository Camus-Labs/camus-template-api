using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace emc.camus.ratelimiting.inmemory.Services
{
    /// <summary>
    /// Provides IP address resolution for rate limiting, accounting for proxies and load balancers.
    /// Checks X-Forwarded-For and X-Real-IP headers for multi-instance deployments behind reverse proxies.
    /// Critical for nginx, Azure LB, CloudFlare, etc.
    /// </summary>
    internal sealed partial class ClientIpResolver
    {
        private const string XForwardedForHeader = "X-Forwarded-For";
        private const string XRealIpHeader = "X-Real-IP";

        private readonly ILogger<ClientIpResolver> _logger;

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Invalid IP format in X-Forwarded-For header: {ForwardedFor}. This may indicate header tampering or proxy misconfiguration. Endpoint: {Path}")]
        private partial void LogInvalidForwardedForIp(string forwardedFor, string path);

        /// <summary>
        /// Creates a new instance of ClientIpResolver with logging support.
        /// </summary>
        /// <param name="logger">Logger for recording IP resolution issues and proxy configuration warnings.</param>
        public ClientIpResolver(ILogger<ClientIpResolver> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        /// <summary>
        /// Resolves the client IP address from the HTTP context, checking proxy headers first.
        /// </summary>
        /// <param name="context">The HTTP context to resolve the IP address from.</param>
        /// <returns>The resolved client IP address as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client IP address cannot be determined from any source.</exception>
        public string GetClientIpAddress(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // Check X-Forwarded-For header (standard for proxies/load balancers)
            var forwardedIp = TryGetForwardedForIp(context);
            if (forwardedIp != null)
            {
                return forwardedIp;
            }

            // Check X-Real-IP header (used by some proxies like nginx)
            var realIp = TryGetRealIp(context);
            if (realIp != null)
            {
                return realIp;
            }

            // Fallback to direct connection IP (only works without proxy)
            var remoteIp = TryGetRemoteIp(context);
            if (remoteIp != null)
            {
                return remoteIp;
            }

            throw new InvalidOperationException(
                $"Unable to determine client IP address for rate limiting. " +
                $"No proxy headers (X-Forwarded-For, X-Real-IP) and no direct connection IP available. " +
                $"Path: {context.Request.Path}, Method: {context.Request.Method}. " +
                $"This indicates a network configuration issue or unsupported proxy setup.");
        }

        private string? TryGetForwardedForIp(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(XForwardedForHeader, out var forwardedFor))
            {
                return null;
            }

            var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length == 0)
            {
                return null;
            }

            var clientIp = ips[0].Trim();
            if (string.IsNullOrWhiteSpace(clientIp))
            {
                return null;
            }

            if (System.Net.IPAddress.TryParse(clientIp, out _))
            {
                return clientIp;
            }

            // Invalid IP format in X-Forwarded-For - log warning
            LogInvalidForwardedForIp(clientIp, context.Request.Path);

            return null;
        }

        private static string? TryGetRealIp(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(XRealIpHeader, out var realIp))
            {
                return null;
            }

            var ip = realIp.ToString().Trim();
            return IsValidIp(ip) ? ip : null;
        }

        private static string? TryGetRemoteIp(HttpContext context)
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (remoteIp == null)
            {
                return null;
            }

            return remoteIp;
        }

        private static bool IsValidIp(string ip)
        {
            return !string.IsNullOrWhiteSpace(ip) && System.Net.IPAddress.TryParse(ip, out _);
        }
    }
}
