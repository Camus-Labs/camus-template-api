using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using emc.camus.api.Utilities;
using emc.camus.api.Middleware;
using emc.camus.api.Configurations;
using emc.camus.api.test.Helpers;
using emc.camus.application.Common;

namespace emc.camus.api.test.Middleware;

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
            context.Items[RateLimitContextKeys.Policy] = policy;
        }

        if (limit != null)
        {
            context.Items[RateLimitContextKeys.Limit] = limit;
        }

        if (window != null)
        {
            context.Items[RateLimitContextKeys.Window] = window;
        }

        return context;
    }

    private RateLimitHeadersMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        return new RateLimitHeadersMiddleware(next, _timeProvider);
    }

    // --- AC-04: InvokeAsync sets rate limit headers ---

    [Fact]
    public async Task InvokeAsync_ValidContext_SetsRateLimitLimitHeader()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitLimit].ToString().Should().Be("100");
    }

    [Fact]
    public async Task InvokeAsync_ValidContext_SetsRateLimitResetHeader()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();
        var expectedReset = _timeProvider.GetUtcNow().AddSeconds(60).ToUnixTimeSeconds();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var resetValue = long.Parse(context.Response.Headers[Headers.RateLimitReset].ToString(), CultureInfo.InvariantCulture);
        resetValue.Should().Be(expectedReset);
    }

    [Fact]
    public async Task InvokeAsync_ValidContext_SetsRateLimitPolicyHeader()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitPolicy].ToString().Should().Be("default");
    }

    [Fact]
    public async Task InvokeAsync_ValidContext_SetsRateLimitWindowHeader()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
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

    [Fact]
    public async Task InvokeAsync_MissingPolicyContextItem_SetsUnknownPolicyHeader()
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
    public async Task InvokeAsync_MissingLimitContextItem_SetsUnknownLimitHeader()
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
    public async Task InvokeAsync_InvalidWindowValue_DoesNotSetResetHeader()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(window: "not-a-number");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitReset].ToString().Should().BeEmpty();
    }
}
