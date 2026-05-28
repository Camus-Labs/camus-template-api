using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using emc.camus.application.Observability;
using emc.camus.observability.otel.Services;

namespace emc.camus.observability.otel.test.Services;

public class ActivitySourceWrapperTests : IDisposable
{
    private const string TestOperationName = "test-op";
    private const string OtelStatusCodeTag = "otel.status_code";
    private const string ExceptionEventName = "exception";
    private const string CacheMissEvent = "cache_miss";
    private const string CredentialsValidatedEvent = "credentials_validated";
    private static readonly Dictionary<string, object?> RequestTags = new() { { "userId", "abc" }, { "count", 5 } };
    private static readonly Dictionary<string, object?> ExecutionTags = new() { { "duration", 42 } };
    private static readonly Dictionary<string, object?> ResponseTags = new() { { "status", 200 } };
    private static readonly Dictionary<string, object?> SingleEntryTags = new() { { "key", "value" } };
    private static readonly Dictionary<string, object?> CacheEventTags = new() { { "cache.key", "user:123" }, { "cache.hit", false } };
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _listener;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ActivitySourceWrapper _wrapper;

    public ActivitySourceWrapperTests()
    {
        _activitySource = new ActivitySource("test-activity-source");
        _timeProvider = new FakeTimeProvider();
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
        _wrapper = new ActivitySourceWrapper(_activitySource, _timeProvider);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _activitySource.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullActivitySource_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new ActivitySourceWrapper(null!, _timeProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("activitySource");
    }

    // --- StartActivity ---

    [Fact]
    public void StartActivity_ValidInputs_ReturnsActivityWithStandardTags()
    {
        // Arrange
        // Act
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem("operation.type").Should().Be("read");
        activity.GetTagItem(OtelStatusCodeTag).Should().Be("UNSET");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StartActivity_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        // Act
        var act = () => _wrapper.StartActivity(name!, OperationType.Read);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void StartActivity_InvalidOperationType_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        var act = () => _wrapper.StartActivity(TestOperationName, (OperationType)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("operationType");
    }

    // --- SetRequestTags ---

    [Fact]
    public void SetRequestTags_WithActivity_AddsPrefixedTags()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        _wrapper.SetRequestTags(activity, RequestTags);

        // Assert
        activity!.GetTagItem("request.userId").Should().Be("abc");
        activity.GetTagItem("request.count").Should().Be(5);
    }

    [Fact]
    public void SetRequestTags_NullActivity_DoesNotThrow()
    {
        // Arrange
        // Act
        var act = () => _wrapper.SetRequestTags(null, SingleEntryTags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetRequestTags_NullTags_ThrowsArgumentNullException()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        var act = () => _wrapper.SetRequestTags(activity, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("tags");
    }

    // --- SetExecutionTags ---

    [Fact]
    public void SetExecutionTags_WithActivity_AddsPrefixedTags()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        _wrapper.SetExecutionTags(activity, ExecutionTags);

        // Assert
        activity!.GetTagItem("execution.duration").Should().Be(42);
    }

    [Fact]
    public void SetExecutionTags_NullActivity_DoesNotThrow()
    {
        // Arrange
        // Act
        var act = () => _wrapper.SetExecutionTags(null, SingleEntryTags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetExecutionTags_NullTags_ThrowsArgumentNullException()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        var act = () => _wrapper.SetExecutionTags(activity, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("tags");
    }

    // --- SetResponseTags ---

    [Fact]
    public void SetResponseTags_WithActivity_AddsPrefixedTags()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        _wrapper.SetResponseTags(activity, ResponseTags);

        // Assert
        activity!.GetTagItem("response.status").Should().Be(200);
    }

    [Fact]
    public void SetResponseTags_NullActivity_DoesNotThrow()
    {
        // Arrange
        // Act
        var act = () => _wrapper.SetResponseTags(null, SingleEntryTags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetResponseTags_NullTags_ThrowsArgumentNullException()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        var act = () => _wrapper.SetResponseTags(activity, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("tags");
    }

    // --- ActivitySucceeded ---

    [Fact]
    public void ActivitySucceeded_WithActivity_SetsStatusCodeToOk()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        _wrapper.ActivitySucceeded(activity);

        // Assert
        activity!.GetTagItem(OtelStatusCodeTag).Should().Be("OK");
    }

    [Fact]
    public void ActivitySucceeded_NullActivity_DoesNotThrow()
    {
        // Arrange
        // Act
        var act = () => _wrapper.ActivitySucceeded(null);

        // Assert
        act.Should().NotThrow();
    }

    // --- ActivityFailed ---

    [Fact]
    public void ActivityFailed_WithActivity_SetsErrorStatusAndAddsExceptionEvent()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);
        var exception = new InvalidOperationException("something broke");

        // Act
        _wrapper.ActivityFailed(activity, exception);

        // Assert
        activity!.GetTagItem(OtelStatusCodeTag).Should().Be("ERROR");
        activity.GetTagItem("otel.status_description").Should().Be("something broke");

        activity.Events.Should().ContainSingle(e => e.Name == ExceptionEventName);
        var exceptionEvent = activity.Events.Single(e => e.Name == ExceptionEventName);
        exceptionEvent.Tags.Should().Contain(t => t.Key == "exception.type" && (string)t.Value! == typeof(InvalidOperationException).FullName);
        exceptionEvent.Tags.Should().Contain(t => t.Key == "exception.message" && (string)t.Value! == "something broke");
    }

    [Fact]
    public void ActivityFailed_NullActivity_DoesNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("something broke");

        // Act
        var act = () => _wrapper.ActivityFailed(null, exception);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ActivityFailed_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        var act = () => _wrapper.ActivityFailed(activity, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("ex");
    }

    // --- AddEvent ---

    [Fact]
    public void AddEvent_WithActivityAndTags_AddsEventWithTags()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        _wrapper.AddEvent(activity, CacheMissEvent, CacheEventTags);

        // Assert
        activity!.Events.Should().ContainSingle(e => e.Name == CacheMissEvent);
        var evt = activity.Events.Single(e => e.Name == CacheMissEvent);
        evt.Tags.Should().Contain(t => t.Key == "cache.key" && (string)t.Value! == "user:123");
        evt.Tags.Should().Contain(t => t.Key == "cache.hit" && (bool)t.Value! == false);
    }

    [Fact]
    public void AddEvent_WithActivityAndNoTags_AddsEventWithoutTags()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        _wrapper.AddEvent(activity, CredentialsValidatedEvent);

        // Assert
        activity!.Events.Should().ContainSingle(e => e.Name == CredentialsValidatedEvent);
        var evt = activity.Events.Single(e => e.Name == CredentialsValidatedEvent);
        evt.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddEvent_NullActivity_DoesNotThrow()
    {
        // Arrange
        // Act
        var act = () => _wrapper.AddEvent(null, "some_event");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddEvent_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        var act = () => _wrapper.AddEvent(activity, name!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    // --- StartActivityAndRunAsync ---

    [Fact]
    public async Task StartActivityAndRunAsync_SuccessfulFunc_ReturnsResultAndSetsStatusToOk()
    {
        // Arrange
        Activity? capturedActivity = null;

        // Act
        var result = await _wrapper.StartActivityAndRunAsync(TestOperationName, OperationType.Read, activity =>
        {
            capturedActivity = activity;
            return Task.FromResult(42);
        });

        // Assert
        result.Should().Be(42);
        capturedActivity!.GetTagItem(OtelStatusCodeTag).Should().Be("OK");
    }

    [Fact]
    public async Task StartActivityAndRunAsync_FailingFunc_RethrowsExceptionAndSetsStatusToError()
    {
        // Arrange
        Activity? capturedActivity = null;
        var expectedException = new InvalidOperationException("boom");

        // Act
        var act = async () =>
        {
            await _wrapper.StartActivityAndRunAsync<int>(TestOperationName, OperationType.Read, activity =>
            {
                capturedActivity = activity;
                throw expectedException;
            });
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*boom*");
        capturedActivity!.GetTagItem(OtelStatusCodeTag).Should().Be("ERROR");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartActivityAndRunAsync_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        // Act
        var act = () => _wrapper.StartActivityAndRunAsync<int>(name!, OperationType.Read, _ => Task.FromResult(1));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartActivityAndRunAsync_InvalidOperationType_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        var act = () => _wrapper.StartActivityAndRunAsync<int>(TestOperationName, (OperationType)999, _ => Task.FromResult(1));

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task StartActivityAndRunAsync_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => _wrapper.StartActivityAndRunAsync<int>(TestOperationName, OperationType.Read, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // --- ActivityCancelled ---

    [Fact]
    public void ActivityCancelled_WithActivity_SetsErrorStatusAndAddsEvent()
    {
        // Arrange
        using var activity = _wrapper.StartActivity(TestOperationName, OperationType.Read);

        // Act
        _wrapper.ActivityCancelled(activity);

        // Assert
        activity!.GetTagItem(OtelStatusCodeTag).Should().Be("ERROR");
        activity.GetTagItem("otel.status_description").Should().Be("Operation cancelled.");
        activity.Events.Should().ContainSingle(e => e.Name == "cancelled");
    }

    [Fact]
    public void ActivityCancelled_NullActivity_DoesNotThrow()
    {
        // Act
        var act = () => _wrapper.ActivityCancelled(null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task StartActivityAndRunAsync_CancelledFunc_RethrowsAndSetsCancelledStatus()
    {
        // Arrange
        Activity? capturedActivity = null;

        // Act
        var act = async () =>
        {
            await _wrapper.StartActivityAndRunAsync<int>(TestOperationName, OperationType.Read, activity =>
            {
                capturedActivity = activity;
                throw new OperationCanceledException("cancelled");
            });
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        capturedActivity!.GetTagItem(OtelStatusCodeTag).Should().Be("ERROR");
        capturedActivity.Events.Should().ContainSingle(e => e.Name == "cancelled");
    }
}
