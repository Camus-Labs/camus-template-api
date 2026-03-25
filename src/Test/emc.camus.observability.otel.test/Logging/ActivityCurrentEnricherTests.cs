using System.Diagnostics;
using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using emc.camus.observability.otel.Logging;

namespace emc.camus.observability.otel.test.Logging;

public class ActivityCurrentEnricherTests
{
    private readonly ActivityCurrentEnricher _enricher = new();
    private readonly ILogEventPropertyFactory _propertyFactory = new LogEventPropertyFactory();

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", []),
            []);
    }

    // --- Enrich: null guards ---

    [Fact]
    public void Enrich_NullLogEvent_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => _enricher.Enrich(null!, _propertyFactory);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logEvent");
    }

    [Fact]
    public void Enrich_NullPropertyFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var act = () => _enricher.Enrich(logEvent, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("propertyFactory");
    }

    // --- Enrich: no Activity ---

    [Fact]
    public void Enrich_NoCurrentActivity_DoesNotAddProperties()
    {
        // Arrange
        Activity.Current = null;
        var logEvent = CreateLogEvent();

        // Act
        _enricher.Enrich(logEvent, _propertyFactory);

        // Assert
        logEvent.Properties.Should().NotContainKey("trace_id");
        logEvent.Properties.Should().NotContainKey("span_id");
    }

    // --- Enrich: with Activity ---

    [Fact]
    public void Enrich_WithCurrentActivity_AddsTraceAndSpanIdProperties()
    {
        // Arrange
        using var source = new ActivitySource("test-enricher");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test-op");
        var logEvent = CreateLogEvent();

        // Act
        _enricher.Enrich(logEvent, _propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey("trace_id");
        logEvent.Properties["trace_id"].ToString().Trim('"')
            .Should().Be(activity!.TraceId.ToHexString());

        logEvent.Properties.Should().ContainKey("span_id");
        logEvent.Properties["span_id"].ToString().Trim('"')
            .Should().Be(activity.SpanId.ToHexString());
    }

    /// <summary>
    /// Minimal ILogEventPropertyFactory for test use.
    /// </summary>
    private sealed class LogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
