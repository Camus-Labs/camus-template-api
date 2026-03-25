using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using emc.camus.ratelimiting.inmemory.Middleware;
using emc.camus.application.Common;

namespace emc.camus.ratelimiting.inmemory.test.Middleware;

public class RateLimitHeadersMiddlewareTests
{
    private const string TestPolicy = "default";
    private const string TestLimit = "100";
    private const string TestWindow = "60";

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

    private static RateLimitHeadersMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new RateLimitHeadersMiddleware(next);
    }

    // --- InvokeAsync: Headers Set ---

    [Fact]
    public async Task InvokeAsync_ValidContext_SetsAllRateLimitHeaders()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitLimit].ToString().Should().Be(TestLimit);
        context.Response.Headers[Headers.RateLimitReset].ToString().Should().NotBeNullOrEmpty();
        context.Response.Headers[Headers.RateLimitPolicy].ToString().Should().Be(TestPolicy);
        context.Response.Headers[Headers.RateLimitWindow].ToString().Should().Be(TestWindow);
    }

    // --- InvokeAsync: Next Delegate ---

    [Fact]
    public async Task InvokeAsync_ValidContext_CallsNextDelegate()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    // --- InvokeAsync: Missing Context Items ---

    [Fact]
    public async Task InvokeAsync_MissingPolicyItem_SetsUnknownPolicy()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(policy: null);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitPolicy].ToString().Should().Be("unknown");
    }

    [Fact]
    public async Task InvokeAsync_MissingLimitItem_SetsUnknownLimit()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(limit: null);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitLimit].ToString().Should().Be("unknown");
    }

    [Fact]
    public async Task InvokeAsync_MissingWindowItem_SetsUnknownWindow()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(window: null);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitWindow].ToString().Should().Be("unknown");
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
        context.Response.Headers.ContainsKey(Headers.RateLimitReset).Should().BeFalse();
    }

    // --- InvokeAsync: Reset Timestamp Calculation ---

    [Fact]
    public async Task InvokeAsync_ValidWindow_SetsResetTimestampInFuture()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();
        var beforeTimestamp = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var afterTimestamp = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds();
        var resetValue = long.Parse(context.Response.Headers[Headers.RateLimitReset].ToString(), CultureInfo.InvariantCulture);
        resetValue.Should().BeGreaterThanOrEqualTo(beforeTimestamp);
        resetValue.Should().BeLessThanOrEqualTo(afterTimestamp);
    }
}
