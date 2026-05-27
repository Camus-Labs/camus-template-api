using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using emc.camus.ratelimiting.inmemory.Middleware;
using emc.camus.ratelimiting.inmemory.test.Helpers;
using emc.camus.application.Common;

namespace emc.camus.ratelimiting.inmemory.test.Middleware;

public class RateLimitHeadersMiddlewareTests
{
    private const string TestPolicy = "default";
    private const string TestLimit = "100";
    private const string TestWindow = "60";

    private readonly FakeTimeProvider _timeProvider;

    public RateLimitHeadersMiddlewareTests()
    {
        _timeProvider = new FakeTimeProvider();
    }

    private static DefaultHttpContext CreateHttpContext(
        string? policy = TestPolicy,
        string? limit = TestLimit,
        string? window = TestWindow)
    {
        var context = new DefaultHttpContext();

        if (policy != null)
        {
            context.Items["RateLimit:Policy"] = policy;
        }

        if (limit != null)
        {
            context.Items["RateLimit:Limit"] = limit;
        }

        if (window != null)
        {
            context.Items["RateLimit:Window"] = window;
        }

        return context;
    }

    private RateLimitHeadersMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new RateLimitHeadersMiddleware(next, _timeProvider);
    }

    // --- InvokeAsync: Headers Set ---

    [Fact]
    public async Task InvokeAsync_ValidContext_SetsAllRateLimitHeaders()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();
        var expectedReset = _timeProvider.GetUtcNow().AddSeconds(60).ToUnixTimeSeconds();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitLimit].ToString().Should().Be("100");
        var resetValue = long.Parse(context.Response.Headers[Headers.RateLimitReset].ToString(), CultureInfo.InvariantCulture);
        resetValue.Should().Be(expectedReset);
        context.Response.Headers[Headers.RateLimitPolicy].ToString().Should().Be("default");
        context.Response.Headers[Headers.RateLimitWindow].ToString().Should().Be("60");
    }

    // --- InvokeAsync: Next Delegate ---

    [Fact]
    public async Task InvokeAsync_ValidContext_CallsNextDelegate()
    {
        // Arrange
        var stub = new NextDelegateStub();
        var middleware = CreateMiddleware(stub.Invoke);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        stub.WasCalled.Should().BeTrue();
    }

    // --- InvokeAsync: Missing Context Items ---

    [Theory]
    [InlineData(null, TestLimit, TestWindow, "RateLimit-Policy")]
    [InlineData(TestPolicy, null, TestWindow, "RateLimit-Limit")]
    [InlineData(TestPolicy, TestLimit, null, "RateLimit-Window")]
    public async Task InvokeAsync_MissingContextItem_SetsUnknownValue(string? policy, string? limit, string? window, string headerName)
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(policy: policy, limit: limit, window: window);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[headerName].ToString().Should().Be("unknown");
    }

    [Fact]
    public async Task InvokeAsync_InvalidWindowValue_DoesNotSetResetHeader()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(window: "not-a-number");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey(Headers.RateLimitReset);
    }


}
