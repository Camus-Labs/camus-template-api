using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using emc.camus.application.Common;
using emc.camus.observability.otel.Middleware;

namespace emc.camus.observability.otel.test.Middleware;

public class TraceIdHeaderMiddlewareTests
{
    private static readonly RequestDelegate NoOpNext = _ => Task.CompletedTask;

    private static (DefaultHttpContext Context, TestResponseFeature Feature) CreateTestContext()
    {
        var feature = new TestResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(feature);
        return (context, feature);
    }

    // --- InvokeAsync: calls next ---

    [Fact]
    public async Task InvokeAsync_Always_CallsNextMiddleware()
    {
        // Arrange
        var wasCalled = false;
        RequestDelegate next = _ => { wasCalled = true; return Task.CompletedTask; };
        var middleware = new TraceIdHeaderMiddleware(next);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        wasCalled.Should().BeTrue();
    }

    // --- InvokeAsync: Trace-Id header from Activity ---

    [Fact]
    public async Task InvokeAsync_WithActivity_SetsTraceIdHeaderFromActivity()
    {
        // Arrange
        using var source = new ActivitySource("test-middleware");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test-op");
        var middleware = new TraceIdHeaderMiddleware(NoOpNext);
        var (context, feature) = CreateTestContext();

        // Act
        await middleware.InvokeAsync(context);
        await feature.FireOnStartingAsync();

        // Assert
        context.Response.Headers.Should().ContainKey(Headers.TraceId);
        context.Response.Headers[Headers.TraceId].ToString()
            .Should().Be(activity!.TraceId.ToString());
    }

    // --- InvokeAsync: Trace-Id header fallback ---

    [Fact]
    public async Task InvokeAsync_NoActivity_SetsTraceIdHeaderFromTraceIdentifier()
    {
        // Arrange
        Activity.Current = null;
        var middleware = new TraceIdHeaderMiddleware(NoOpNext);
        var (context, feature) = CreateTestContext();
        context.TraceIdentifier = "fallback-trace-id";

        // Act
        await middleware.InvokeAsync(context);
        await feature.FireOnStartingAsync();

        // Assert
        context.Response.Headers.Should().ContainKey(Headers.TraceId);
        context.Response.Headers[Headers.TraceId].ToString()
            .Should().Be("fallback-trace-id");
    }

    // --- InvokeAsync: does not overwrite existing header ---

    [Fact]
    public async Task InvokeAsync_HeaderAlreadyExists_DoesNotOverwrite()
    {
        // Arrange
        Activity.Current = null;
        var middleware = new TraceIdHeaderMiddleware(NoOpNext);
        var (context, feature) = CreateTestContext();
        context.TraceIdentifier = "new-trace-id";
        context.Response.Headers[Headers.TraceId] = "existing-trace-id";

        // Act
        await middleware.InvokeAsync(context);
        await feature.FireOnStartingAsync();

        // Assert
        context.Response.Headers[Headers.TraceId].ToString()
            .Should().Be("existing-trace-id");
    }

    private sealed class TestResponseFeature : IHttpResponseFeature
    {
        private Func<object, Task>? _callback;
        private object? _state;

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            _callback = callback;
            _state = state;
        }

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public async Task FireOnStartingAsync()
        {
            if (_callback != null)
            {
                HasStarted = true;
                await _callback(_state!);
            }
        }
    }
}
