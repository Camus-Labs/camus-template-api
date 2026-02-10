using System.Diagnostics;
using emc.camus.application.Observability;
using emc.camus.observability.otel.Telemetry;
using FluentAssertions;

namespace emc.camus.observability.otel.test.Telemetry;

/// <summary>
/// Unit tests for ActivitySourceWrapper to verify tracing logic and behavior.
/// </summary>
public class ActivitySourceWrapperTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivitySourceWrapper _wrapper;
    private readonly ActivityListener _listener;

    public ActivitySourceWrapperTests()
    {
        _activitySource = new ActivitySource("TestActivitySource");
        _wrapper = new ActivitySourceWrapper(_activitySource);

        // Create a listener to enable activity creation
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void Constructor_WithNullActivitySource_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ActivitySourceWrapper(null);
        act.Should().Throw<ArgumentNullException>().WithParameterName("activitySource");
    }

    [Fact]
    public void StartActivity_ShouldSetOperationTypeTag()
    {
        // Act
        using var activity = _wrapper.StartActivity("test-operation", OperationType.Read);

        // Assert
        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(new KeyValuePair<string, string?>("operation.type", "read"));
    }

    [Fact]
    public void StartActivity_ShouldSetStatusCodeToUnset()
    {
        // Act
        using var activity = _wrapper.StartActivity("test-operation", OperationType.Auth);

        // Assert
        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(new KeyValuePair<string, string?>("otel.status_code", "UNSET"));
    }

    [Theory]
    [InlineData(OperationType.Read, "read")]        // Read operation
    [InlineData(OperationType.Auth, "auth")]        // Authentication operation
    [InlineData(OperationType.Create, "create")]    // Create operation
    [InlineData(OperationType.Update, "update")]    // Update operation
    [InlineData(OperationType.Delete, "delete")]    // Delete operation
    public void StartActivity_WithVariousOperationTypes_ShouldSetCorrectTag(OperationType opType, string expected)
    {
        // Act
        using var activity = _wrapper.StartActivity("test", opType);

        // Assert
        activity!.Tags.Should().Contain(new KeyValuePair<string, string?>("operation.type", expected));
    }

    [Fact]
    public void SetRequestTags_ShouldPrefixWithRequest()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);
        var tags = new Dictionary<string, object?>
        {
            { "userId", "user123" },
            { "method", "GET" }
        };

        // Act
        _wrapper.SetRequestTags(activity, tags);

        // Assert
        activity!.GetTagItem("request.userId").Should().Be("user123");
        activity.GetTagItem("request.method").Should().Be("GET");
    }

    [Fact]
    public void SetRequestTags_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var tags = new Dictionary<string, object?> { { "key", "value" } };

        // Act & Assert
        var act = () => _wrapper.SetRequestTags(null, tags);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetRequestTags_WithNullTags_ShouldNotThrow()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);

        // Act & Assert
        var act = () => _wrapper.SetRequestTags(activity, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetRequestTags_WithEmptyDictionary_ShouldNotThrow()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);
        var emptyTags = new Dictionary<string, object?>();

        // Act & Assert
        var act = () => _wrapper.SetRequestTags(activity, emptyTags);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetResponseTags_ShouldPrefixWithResponse()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);
        var tags = new Dictionary<string, object?>
        {
            { "statusCode", 200 },
            { "size", 1024 }
        };

        // Act
        _wrapper.SetResponseTags(activity, tags);

        // Assert
        activity!.GetTagItem("response.statusCode").Should().Be(200);
        activity.GetTagItem("response.size").Should().Be(1024);
    }

    [Fact]
    public void SetResponseTags_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var tags = new Dictionary<string, object?> { { "key", "value" } };

        // Act & Assert
        var act = () => _wrapper.SetResponseTags(null, tags);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetResponseTags_WithNullTags_ShouldNotThrow()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);

        // Act & Assert
        var act = () => _wrapper.SetResponseTags(activity, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetResponseTags_WithEmptyDictionary_ShouldNotThrow()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);
        var emptyTags = new Dictionary<string, object?>();

        // Act & Assert
        var act = () => _wrapper.SetResponseTags(activity, emptyTags);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetRequestTags_WithMultipleTagsAndNullValues_ShouldSetAllTags()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);
        var tags = new Dictionary<string, object?>
        {
            { "validKey", "validValue" },
            { "nullKey", null },
            { "intKey", 123 }
        };

        // Act
        _wrapper.SetRequestTags(activity, tags);

        // Assert
        activity!.GetTagItem("request.validKey").Should().Be("validValue");
        activity.GetTagItem("request.nullKey").Should().BeNull();
        activity.GetTagItem("request.intKey").Should().Be(123);
    }

    [Fact]
    public void SetResponseTags_WithMultipleTagsAndNullValues_ShouldSetAllTags()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);
        var tags = new Dictionary<string, object?>
        {
            { "validKey", "validValue" },
            { "nullKey", null },
            { "boolKey", true }
        };

        // Act
        _wrapper.SetResponseTags(activity, tags);

        // Assert
        activity!.GetTagItem("response.validKey").Should().Be("validValue");
        activity.GetTagItem("response.nullKey").Should().BeNull();
        activity.GetTagItem("response.boolKey").Should().Be(true);
    }

    [Fact]
    public void ActivitySucceeded_ShouldSetStatusCodeToOk()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Auth);

        // Act
        _wrapper.ActivitySucceeded(activity);

        // Assert
        activity!.Tags.Should().Contain(new KeyValuePair<string, string?>("otel.status_code", "OK"));
    }

    [Fact]
    public void ActivitySucceeded_WithNullActivity_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _wrapper.ActivitySucceeded(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void ActivityFailed_ShouldSetStatusCodeToError()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);
        var exception = new InvalidOperationException("Test error");

        // Act
        _wrapper.ActivityFailed(activity, exception);

        // Assert
        activity!.Tags.Should().Contain(new KeyValuePair<string, string?>("otel.status_code", "ERROR"));
        activity.Tags.Should().Contain(new KeyValuePair<string, string?>("otel.status_description", "Test error"));
    }

    [Fact]
    public void ActivityFailed_ShouldAddExceptionEvent()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);
        var exception = new InvalidOperationException("Test error");

        // Act
        _wrapper.ActivityFailed(activity, exception);

        // Assert
        activity!.Events.Should().Contain(e => e.Name == "exception");
        var exceptionEvent = activity.Events.First(e => e.Name == "exception");
        exceptionEvent.Tags.Should().Contain(new KeyValuePair<string, object?>("exception.type", typeof(InvalidOperationException).FullName));
        exceptionEvent.Tags.Should().Contain(new KeyValuePair<string, object?>("exception.message", "Test error"));
    }

    [Fact]
    public void ActivityFailed_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        var exception = new Exception("Test");

        // Act & Assert
        var act = () => _wrapper.ActivityFailed(null, exception);
        act.Should().NotThrow();
    }

    [Fact]
    public void ActivityFailed_WithNullException_ShouldNotThrow()
    {
        // Arrange
        using var activity = _wrapper.StartActivity("test", OperationType.Read);

        // Act & Assert
        var act = () => _wrapper.ActivityFailed(activity, null);
        act.Should().NotThrow();

        // Should still set status code to ERROR
        activity!.Tags.Should().Contain(new KeyValuePair<string, string?>("otel.status_code", "ERROR"));
    }

    [Fact]
    public async Task StartActivityAndRunAsync_WithSuccessfulOperation_ShouldMarkAsSucceeded()
    {
        // Arrange
        var expectedResult = 42;

        // Act
        var result = await _wrapper.StartActivityAndRunAsync("test", OperationType.Read, async activity =>
        {
            await Task.Delay(10);
            activity.Should().NotBeNull();
            return expectedResult;
        });

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task StartActivityAndRunAsync_WithFailedOperation_ShouldMarkAsFailedAndThrow()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Operation failed");

        // Act & Assert
        var act = async () => await _wrapper.StartActivityAndRunAsync<int>("test", OperationType.Auth, async _ =>
        {
            await Task.Delay(10);
            throw expectedException;
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Operation failed");
    }

    [Fact]
    public async Task StartActivityAndRunAsync_ShouldExecuteFunctionWithActivity()
    {
        // Arrange
        Activity? capturedActivity = null;

        // Act
        await _wrapper.StartActivityAndRunAsync("test", OperationType.Info, async activity =>
        {
            capturedActivity = activity;
            await Task.Delay(10);
            return "result";
        });

        // Assert
        capturedActivity.Should().NotBeNull();
        capturedActivity!.OperationName.Should().Be("test");
    }

    public void Dispose()
    {
        _listener?.Dispose();
        _activitySource?.Dispose();
    }
}
