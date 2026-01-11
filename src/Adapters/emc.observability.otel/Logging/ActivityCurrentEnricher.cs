using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace emc.camus.observability.otel.Logging
{
    public sealed class ActivityCurrentEnricher : ILogEventEnricher
    {
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
