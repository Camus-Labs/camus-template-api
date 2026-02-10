using System.Diagnostics;
using emc.camus.observability.otel.Logging;
using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Core;

namespace emc.camus.observability.otel.test.Logging;

/// <summary>
/// Unit tests for ActivityCurrentEnricher to verify trace context enrichment.
/// </summary>
public class ActivityCurrentEnricherTests
{
    private readonly ActivityCurrentEnricher _enricher;

    public ActivityCurrentEnricherTests()
    {
        _enricher = new ActivityCurrentEnricher();
    }

    [Fact]
    public void Enrich_WithActiveActivity_ShouldAddTraceIdProperty()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestActivity");
        var expectedTraceId = activity!.TraceId.ToHexString();

        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey("trace_id");
        var traceIdProperty = logEvent.Properties["trace_id"] as ScalarValue;
        traceIdProperty.Should().NotBeNull();
        traceIdProperty!.Value.Should().Be(expectedTraceId);

        listener.Dispose();
        activitySource.Dispose();
    }

    [Fact]
    public void Enrich_WithActiveActivity_ShouldAddSpanIdProperty()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestActivity");
        var expectedSpanId = activity!.SpanId.ToHexString();

        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey("span_id");
        var spanIdProperty = logEvent.Properties["span_id"] as ScalarValue;
        spanIdProperty.Should().NotBeNull();
        spanIdProperty!.Value.Should().Be(expectedSpanId);

        listener.Dispose();
        activitySource.Dispose();
    }

    [Fact]
    public void Enrich_WithActiveActivity_ShouldAddBothTraceAndSpanIds()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestActivity");
        
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey("trace_id");
        logEvent.Properties.Should().ContainKey("span_id");
        logEvent.Properties.Should().HaveCount(2);

        listener.Dispose();
        activitySource.Dispose();
    }

    [Fact]
    public void Enrich_WithoutActiveActivity_ShouldNotAddProperties()
    {
        // Arrange
        Activity.Current = null;
        
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().NotContainKey("trace_id");
        logEvent.Properties.Should().NotContainKey("span_id");
        logEvent.Properties.Should().BeEmpty();
    }

    [Fact]
    public void Enrich_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity.Current = null;
        
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        var act = () => _enricher.Enrich(logEvent, propertyFactory);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Enrich_MultipleCallsWithSameActivity_ShouldUseSameIds()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestActivity");
        var expectedTraceId = activity!.TraceId.ToHexString();
        var expectedSpanId = activity.SpanId.ToHexString();

        var logEvent1 = CreateLogEvent();
        var logEvent2 = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent1, propertyFactory);
        _enricher.Enrich(logEvent2, propertyFactory);

        // Assert
        var traceId1 = ((ScalarValue)logEvent1.Properties["trace_id"]).Value;
        var traceId2 = ((ScalarValue)logEvent2.Properties["trace_id"]).Value;
        var spanId1 = ((ScalarValue)logEvent1.Properties["span_id"]).Value;
        var spanId2 = ((ScalarValue)logEvent2.Properties["span_id"]).Value;

        traceId1.Should().Be(expectedTraceId);
        traceId2.Should().Be(expectedTraceId);
        spanId1.Should().Be(expectedSpanId);
        spanId2.Should().Be(expectedSpanId);

        listener.Dispose();
        activitySource.Dispose();
    }

    [Fact]
    public void Enrich_WithNestedActivities_ShouldUseCurrentActivityIds()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var parentActivity = activitySource.StartActivity("ParentActivity");
        using var childActivity = activitySource.StartActivity("ChildActivity");
        
        var expectedTraceId = childActivity!.TraceId.ToHexString();
        var expectedSpanId = childActivity.SpanId.ToHexString();

        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, propertyFactory);

        // Assert
        var traceId = ((ScalarValue)logEvent.Properties["trace_id"]).Value;
        var spanId = ((ScalarValue)logEvent.Properties["span_id"]).Value;

        traceId.Should().Be(expectedTraceId);
        spanId.Should().Be(expectedSpanId);
        spanId.Should().NotBe(parentActivity!.SpanId.ToHexString());

        listener.Dispose();
        activitySource.Dispose();
    }

    [Fact]
    public void Enrich_WithExistingProperties_ShouldAddTraceProperties()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestActivity");
        
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();
        
        // Add existing property
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("existing_prop", "existing_value"));

        // Act
        _enricher.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey("existing_prop");
        logEvent.Properties.Should().ContainKey("trace_id");
        logEvent.Properties.Should().ContainKey("span_id");
        logEvent.Properties.Should().HaveCount(3);

        listener.Dispose();
        activitySource.Dispose();
    }

    [Fact]
    public void Enrich_UpdatesExistingTraceIdProperty()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestActivity");
        var expectedTraceId = activity!.TraceId.ToHexString();
        
        var logEvent = CreateLogEvent();
        var propertyFactory = new TestPropertyFactory();
        
        // Add old trace_id
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace_id", "old-trace-id"));

        // Act
        _enricher.Enrich(logEvent, propertyFactory);

        // Assert
        var traceId = ((ScalarValue)logEvent.Properties["trace_id"]).Value;
        traceId.Should().Be(expectedTraceId);
        traceId.Should().NotBe("old-trace-id");

        listener.Dispose();
        activitySource.Dispose();
    }

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate(new List<MessageTemplateToken>()),
            new List<LogEventProperty>());
    }

    private class TestPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
