using System.Diagnostics;
using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using emc.camus.observability.otel.Logging;
using emc.camus.observability.otel.test.Helpers;

namespace emc.camus.observability.otel.test.Logging;

public class ActivityCurrentEnricherTests : IDisposable
{
    private const string TraceIdProperty = "trace_id";
    private const string SpanIdProperty = "span_id";
    private readonly Activity? _priorActivity;
    private readonly ActivityCurrentEnricher _enricher;
    private readonly ILogEventPropertyFactory _propertyFactory;

    public ActivityCurrentEnricherTests()
    {
        _priorActivity = Activity.Current;
        _enricher = new ActivityCurrentEnricher();
        _propertyFactory = new LogEventPropertyFactory();
    }

    public void Dispose()
    {
        Activity.Current = _priorActivity;
        GC.SuppressFinalize(this);
    }

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UnixEpoch,
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
        logEvent.Properties.Should().NotContainKey(TraceIdProperty);
        logEvent.Properties.Should().NotContainKey(SpanIdProperty);
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
        logEvent.Properties.Should().ContainKey(TraceIdProperty);
        logEvent.Properties[TraceIdProperty].ToString().Trim('"')
            .Should().Be(activity!.TraceId.ToHexString());

        logEvent.Properties.Should().ContainKey(SpanIdProperty);
        logEvent.Properties[SpanIdProperty].ToString().Trim('"')
            .Should().Be(activity.SpanId.ToHexString());
    }

}
