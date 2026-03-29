using System.Diagnostics;
using FluentAssertions;
using emc.camus.application.Observability;
using emc.camus.observability.otel.Services;

namespace emc.camus.observability.otel.test.Services;

public class ActivitySourceWrapperTests : IDisposable
{
    private readonly ActivitySource _activitySource = new("test-activity-source");
    private readonly ActivityListener _listener;
    private readonly ActivitySourceWrapper _wrapper;

    public ActivitySourceWrapperTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
        _wrapper = new ActivitySourceWrapper(_activitySource);
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
        var act = () => new ActivitySourceWrapper(null!);

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
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem("operation.type").Should().Be("read");
        activity.GetTagItem("otel.status_code").Should().Be("UNSET");
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
        var act = () => _wrapper.StartActivity("test-op", (OperationType)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("operationType");
    }

    // --- SetRequestTags ---

    [Fact]
    public void SetRequestTags_WithActivity_AddsPrefixedTags()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);
        var tags = new Dictionary<string, object?> { { "userId", "abc" }, { "count", 5 } };

        // Act
        _wrapper.SetRequestTags(activity, tags);

        // Assert
        activity!.GetTagItem("request.userId").Should().Be("abc");
        activity.GetTagItem("request.count").Should().Be(5);
    }

    [Fact]
    public void SetRequestTags_NullActivity_DoesNotThrow()
    {
        // Arrange
        var tags = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        var act = () => _wrapper.SetRequestTags(null, tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetRequestTags_NullTags_ThrowsArgumentNullException()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);

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
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);
        var tags = new Dictionary<string, object?> { { "duration", 42 } };

        // Act
        _wrapper.SetExecutionTags(activity, tags);

        // Assert
        activity!.GetTagItem("execution.duration").Should().Be(42);
    }

    [Fact]
    public void SetExecutionTags_NullActivity_DoesNotThrow()
    {
        // Arrange
        var tags = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        var act = () => _wrapper.SetExecutionTags(null, tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetExecutionTags_NullTags_ThrowsArgumentNullException()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);

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
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);
        var tags = new Dictionary<string, object?> { { "status", 200 } };

        // Act
        _wrapper.SetResponseTags(activity, tags);

        // Assert
        activity!.GetTagItem("response.status").Should().Be(200);
    }

    [Fact]
    public void SetResponseTags_NullActivity_DoesNotThrow()
    {
        // Arrange
        var tags = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        var act = () => _wrapper.SetResponseTags(null, tags);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetResponseTags_NullTags_ThrowsArgumentNullException()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);

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
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);

        // Act
        _wrapper.ActivitySucceeded(activity);

        // Assert
        activity!.GetTagItem("otel.status_code").Should().Be("OK");
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
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);
        var exception = new InvalidOperationException("something broke");

        // Act
        _wrapper.ActivityFailed(activity, exception);

        // Assert
        activity!.GetTagItem("otel.status_code").Should().Be("ERROR");
        activity.GetTagItem("otel.status_description").Should().Be("something broke");

        activity.Events.Should().ContainSingle(e => e.Name == "exception");
        var exceptionEvent = activity.Events.Single(e => e.Name == "exception");
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
        using var activity = _wrapper.StartActivity("test-op", OperationType.Read);

        // Act
        var act = () => _wrapper.ActivityFailed(activity, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("ex");
    }

    // --- StartActivityAndRunAsync ---

    [Fact]
    public async Task StartActivityAndRunAsync_SuccessfulFunc_ReturnsResultAndSetsStatusToOk()
    {
        // Arrange
        Activity? capturedActivity = null;

        // Act
        var result = await _wrapper.StartActivityAndRunAsync("test-op", OperationType.Read, activity =>
        {
            capturedActivity = activity;
            return Task.FromResult(42);
        });

        // Assert
        result.Should().Be(42);
        capturedActivity!.GetTagItem("otel.status_code").Should().Be("OK");
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
            await _wrapper.StartActivityAndRunAsync<int>("test-op", OperationType.Read, activity =>
            {
                capturedActivity = activity;
                throw expectedException;
            });
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*boom*");
        capturedActivity!.GetTagItem("otel.status_code").Should().Be("ERROR");
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
        var act = () => _wrapper.StartActivityAndRunAsync<int>("test-op", (OperationType)999, _ => Task.FromResult(1));

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task StartActivityAndRunAsync_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => _wrapper.StartActivityAndRunAsync<int>("test-op", OperationType.Read, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
