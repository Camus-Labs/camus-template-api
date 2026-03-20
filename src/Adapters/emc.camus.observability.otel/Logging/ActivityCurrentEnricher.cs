using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace emc.camus.observability.otel.Logging
{
    /// <summary>
    /// Enriches Serilog log events with current Activity trace and span IDs for distributed tracing correlation.
    /// </summary>
    internal sealed class ActivityCurrentEnricher : ILogEventEnricher
    {
        /// <summary>
        /// Adds trace_id and span_id properties to the log event if an Activity is present.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating log event properties.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            ArgumentNullException.ThrowIfNull(logEvent);
            ArgumentNullException.ThrowIfNull(propertyFactory);

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
