using Serilog.Core;
using Serilog.Events;

namespace emc.camus.observability.otel.test.Helpers;

/// <summary>
/// Minimal <see cref="ILogEventPropertyFactory"/> for test use.
/// </summary>
internal sealed class LogEventPropertyFactory : ILogEventPropertyFactory
{
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
    {
        return new LogEventProperty(name, new ScalarValue(value));
    }
}
