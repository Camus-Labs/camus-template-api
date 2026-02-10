using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace emc.camus.ratelimiting.memory.Services
{
    /// <summary>
    /// Provides IP address resolution for rate limiting, accounting for proxies and load balancers.
    /// This interface is defined in the adapter (not Application layer) because:
    /// 1) It's tightly coupled to HTTP infrastructure (HttpContext)
    /// 2) Only used within this adapter's implementation
    /// 3) Not intended to be shared across multiple adapters
    /// If building a Redis or other adapter, they would use the same rate limit approach.
    /// </summary>
    public interface IClientIpResolver
    {
        /// <summary>
        /// Extracts the client's real IP address from the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context containing request headers and connection info.</param>
        /// <returns>The client's IP address as a string, or "unknown" if unavailable.</returns>
        string GetClientIpAddress(HttpContext context);
    }

    /// <summary>
    /// Default implementation that checks X-Forwarded-For and X-Real-IP headers.
    /// Critical for multi-instance deployments behind reverse proxies (nginx, Azure LB, CloudFlare, etc.).
    /// </summary>
    public class ClientIpResolver : IClientIpResolver
    {
        private readonly ILogger<ClientIpResolver> _logger;
        private static int _hasLoggedDirectConnection = 0; // 0 = not logged, 1 = logged
        private static int _hasLoggedUnknownIp = 0; // 0 = not logged, 1 = logged

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
            
            // Unable to determine IP - log once
            LogUnknownIp(context);
            return "unknown";
        }

        private string? TryGetForwardedForIp(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
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
                "Invalid IP format in X-Forwarded-For header: {ForwardedFor}. " +
                "This may indicate header tampering or proxy misconfiguration. " +
                "Endpoint: {Path}",
                clientIp, context.Request.Path);
            
            return null;
        }

        private string? TryGetRealIp(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
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

            LogDirectConnectionOnce(remoteIp, context);
            return remoteIp;
        }

        private void LogDirectConnectionOnce(string remoteIp, HttpContext context)
        {
            if (Interlocked.CompareExchange(ref _hasLoggedDirectConnection, 1, 0) == 0)
            {
                _logger.LogInformation(
                    "Rate limiting using direct connection IP address: {RemoteIp} (no X-Forwarded-For or X-Real-IP proxy headers detected). " +
                    "This is normal for development, testing, or direct connections. " +
                    "If this is production behind a reverse proxy (nginx, HAProxy, CDN), verify: " +
                    "1) Proxy forwards X-Forwarded-For header, " +
                    "2) UseForwardedHeaders() is configured in Program.cs. " +
                    "Otherwise, all requests from the same proxy share one rate limit. " +
                    "First occurrence: {Path}",
                    remoteIp, context.Request.Path);
            }
        }

        private void LogUnknownIp(HttpContext context)
        {
            if (Interlocked.CompareExchange(ref _hasLoggedUnknownIp, 1, 0) == 0)
            {
                _logger.LogWarning(
                    "Unable to determine client IP address for rate limiting. " +
                    "No proxy headers and no direct connection IP available. " +
                    "First occurrence: {Path}",
                    context.Request.Path);
            }
        }

        private static bool IsValidIp(string ip)
        {
            return !string.IsNullOrWhiteSpace(ip) && System.Net.IPAddress.TryParse(ip, out _);
        }
    }
}
