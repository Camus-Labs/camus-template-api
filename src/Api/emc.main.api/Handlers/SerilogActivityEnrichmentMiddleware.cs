using System.Diagnostics;
using Serilog.Context;

namespace emc.camus.main.api.Handlers;

/// <summary>
/// Middleware that adds <c>trace_id</c> and <c>span_id</c> to Serilog's <see cref="LogContext"/> per HTTP request
/// using <see cref="Activity.Current"/>.
/// </summary>
/// <remarks>
/// Place early in the pipeline so downstream logs include the IDs. For background/non-HTTP work, IDs appear only
/// when an Activity is active; create one or push properties manually if needed.
/// </remarks>
public sealed class SerilogActivityEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Creates the middleware.</summary>
    /// <param name="next">Next delegate in the pipeline.</param>
    public SerilogActivityEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Pushes correlation IDs into <see cref="LogContext"/> for this request.</summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <returns>The task representing the request.</returns>
    public async Task Invoke(HttpContext context)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            using (LogContext.PushProperty("trace_id", activity.TraceId.ToHexString()))
            using (LogContext.PushProperty("span_id", activity.SpanId.ToHexString()))
            {
                await _next(context);
                return;
            }
        }

        await _next(context);
    }
}
