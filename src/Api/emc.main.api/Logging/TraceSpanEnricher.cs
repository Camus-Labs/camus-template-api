using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace emc.camus.main.api.Logging
{
    /// <summary>
    /// Enriches log events with OpenTelemetry correlation identifiers from Activity.Current.
    /// Adds properties: trace_id, span_id.
    /// </summary>
    public sealed class TraceSpanEnricher : ILogEventEnricher
    {
        /// <summary>
        /// Adds correlation identifiers to the given <see cref="LogEvent"/> when an
        /// <see cref="Activity"/> is present.
        /// </summary>
        /// <param name="logEvent">The log event being enriched.</param>
        /// <param name="propertyFactory">Factory used to create log event properties.</param>
        /// <remarks>
        /// When <see cref="Activity.Current"/> is non-null, two properties are added:
        /// <list type="bullet">
        /// <item><description><c>trace_id</c> – The current activity's trace identifier.</description></item>
        /// <item><description><c>span_id</c> – The current activity's span identifier.</description></item>
        /// </list>
        /// If no activity is present, the method returns without modifying the event.
        /// </remarks>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return;
            }

            var traceIdProp = propertyFactory.CreateProperty("trace_id", activity.TraceId.ToString());
            var spanIdProp = propertyFactory.CreateProperty("span_id", activity.SpanId.ToString());

            logEvent.AddPropertyIfAbsent(traceIdProp);
            logEvent.AddPropertyIfAbsent(spanIdProp);
        }
    }
}
