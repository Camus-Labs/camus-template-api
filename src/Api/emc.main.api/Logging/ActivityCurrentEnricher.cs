using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace emc.camus.main.api.Logging
{
    /// <summary>
    /// Per-log enricher that reads Activity.Current at log event time and adds
    /// trace_id and span_id properties for precise correlation with the active span.
    /// </summary>
    public sealed class ActivityCurrentEnricher : ILogEventEnricher
    {
        /// <summary>
        /// Adds <c>trace_id</c> and <c>span_id</c> properties to the given log event by reading
        /// the current <see cref="Activity"/> at log time (<see cref="Activity.Current"/>).
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory used to create structured log properties.</param>
        /// <remarks>
        /// If no <see cref="Activity"/> is active (<c>Activity.Current</c> is <c>null</c>),
        /// this method does nothing. The enricher does not start or stop activities; it only
        /// reflects the currently active span for precise correlation. 
        /// </remarks>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return;
            }

            var traceId = activity.TraceId.ToHexString();
            var spanId = activity.SpanId.ToHexString();

            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("trace_id", traceId));
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("span_id", spanId));
        }
    }
}