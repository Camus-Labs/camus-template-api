using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace emc.camus.observability.otel.Middleware
{
    /// <summary>
    /// Adds an X-Trace-Id header to every HTTP response so clients can correlate
    /// requests with traces. Uses Activity.Current.TraceId when available, falling
    /// back to HttpContext.TraceIdentifier.
    /// </summary>
    internal sealed class ResponseTraceIdMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Creates the middleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public ResponseTraceIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Adds the X-Trace-Id header on the response.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
                if (!string.IsNullOrWhiteSpace(traceId) && !context.Response.Headers.ContainsKey("X-Trace-Id"))
                {
                    context.Response.Headers["X-Trace-Id"] = traceId;
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
