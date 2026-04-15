using emc.camus.application.Common;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace emc.camus.observability.otel.Middleware
{
    /// <summary>
    /// Adds Trace-Id header to every HTTP response so clients can correlate
    /// requests with traces. Uses Activity.Current.TraceId when available, falling
    /// back to HttpContext.TraceIdentifier.
    /// </summary>
    internal sealed class TraceIdHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Creates the middleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public TraceIdHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Adds a Trace-Id header on the response.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Capture trace ID early to avoid issues with auto-generated TraceIdentifier
            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                if (!headers.ContainsKey(Headers.TraceId))
                {
                    headers[Headers.TraceId] = traceId;
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
