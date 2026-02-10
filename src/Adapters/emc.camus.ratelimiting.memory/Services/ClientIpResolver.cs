using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace emc.camus.ratelimiting.memory.Services
{
    /// <summary>
    /// Provides IP address resolution for rate limiting, accounting for proxies and load balancers.
    /// Checks X-Forwarded-For and X-Real-IP headers for multi-instance deployments behind reverse proxies.
    /// Critical for nginx, Azure LB, CloudFlare, etc.
    /// </summary>
    public class ClientIpResolver
    {
        private const string XForwardedForHeader = "X-Forwarded-For";
        private const string XRealIpHeader = "X-Real-IP";
        
        private readonly ILogger<ClientIpResolver> _logger;

        /// <summary>
        /// Creates a new instance of ClientIpResolver with logging support.
        /// </summary>
        /// <param name="logger">Logger for recording IP resolution issues and proxy configuration warnings.</param>
        public ClientIpResolver(ILogger<ClientIpResolver> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public string GetClientIpAddress(HttpContext context)
        {
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
            
            // Unable to determine IP - fail fast (should never happen in production)
            _logger.LogError(
                "Unable to determine client IP address for rate limiting. Rejecting request. " +
                "No proxy headers and no direct connection IP available. " +
                "Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);
            throw new InvalidOperationException("Unable to determine client IP address for rate limiting.");
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
            _logger.LogWarning(
                $"Invalid IP format in {XForwardedForHeader} header: {{ForwardedFor}}. " +
                "This may indicate header tampering or proxy misconfiguration. " +
                "Endpoint: {Path}",
                clientIp, context.Request.Path);
            
            return null;
        }

        private string? TryGetRealIp(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(XRealIpHeader, out var realIp))
            {
                return null;
            }

            var ip = realIp.ToString().Trim();
            return IsValidIp(ip) ? ip : null;
        }

        private string? TryGetRemoteIp(HttpContext context)
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
