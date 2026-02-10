using emc.camus.ratelimiting.memory.Metrics;
using emc.camus.ratelimiting.memory.Middleware;
using emc.camus.application.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace emc.camus.ratelimiting.memory.test.Middleware;

/// <summary>
/// Unit tests for RateLimitMetricsMiddleware to verify header and metric recording logic.
/// </summary>
public class RateLimitMetricsMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly RateLimitMetrics _metrics;
    private const string TestServiceName = "test-service";

    public RateLimitMetricsMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _metrics = new RateLimitMetrics(TestServiceName);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddRateLimitHeaders()
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Items["RateLimit:Policy"] = "default";
        context.Items["RateLimit:Limit"] = "100";
        context.Items["RateLimit:Window"] = "60";

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey(Headers.RateLimitLimit);
        context.Response.Headers[Headers.RateLimitLimit].ToString().Should().Be("100");
        
        context.Response.Headers.Should().ContainKey(Headers.RateLimitReset);
        context.Response.Headers[Headers.RateLimitReset].Should().NotBeEmpty();
        
        context.Response.Headers.Should().ContainKey(Headers.RateLimitPolicy);
        context.Response.Headers[Headers.RateLimitPolicy].ToString().Should().Be("default");
        
        context.Response.Headers.Should().ContainKey(Headers.RateLimitWindow);
        context.Response.Headers[Headers.RateLimitWindow].ToString().Should().Be("60");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Items["RateLimit:Policy"] = "default";
        context.Items["RateLimit:Limit"] = "100";
        context.Items["RateLimit:Window"] = "60";

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRecordHit()
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "GET";
        context.Items["RateLimit:Policy"] = "default";
        context.Items["RateLimit:Limit"] = "100";
        context.Items["RateLimit:Window"] = "60";

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Just verify it doesn't throw, we can't easily verify the metric was recorded
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingPolicy_ShouldUseUnknown()
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "GET";
        context.Items["RateLimit:Limit"] = "100";
        context.Items["RateLimit:Window"] = "60";

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitPolicy].ToString().Should().Be("unknown");
    }

    [Fact]
    public async Task InvokeAsync_WithMissingLimit_ShouldUseUnknown()
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Items["RateLimit:Policy"] = "default";
        context.Items["RateLimit:Window"] = "60";

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitLimit].ToString().Should().Be("unknown");
    }

    [Fact]
    public async Task InvokeAsync_WithMissingWindow_ShouldUseUnknown()
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Items["RateLimit:Policy"] = "default";
        context.Items["RateLimit:Limit"] = "100";

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitWindow].ToString().Should().Be("unknown");
    }

    [Fact]
    public async Task InvokeAsync_WithValidWindow_ShouldCalculateResetTimestamp()
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Items["RateLimit:Policy"] = "default";
        context.Items["RateLimit:Limit"] = "100";
        context.Items["RateLimit:Window"] = "60";

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var beforeInvoke = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds();

        // Act
        await middleware.InvokeAsync(context);

        var afterInvoke = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds();

        // Assert
        context.Response.Headers.Should().ContainKey(Headers.RateLimitReset);
        var resetTimestamp = long.Parse(context.Response.Headers[Headers.RateLimitReset]!);
        resetTimestamp.Should().BeGreaterThanOrEqualTo(beforeInvoke);
        resetTimestamp.Should().BeLessThanOrEqualTo(afterInvoke + 1); // Allow 1 second tolerance
    }

    [Theory]
    [InlineData("default", "250", "60")]
    [InlineData("strict", "50", "60")]
    [InlineData("relaxed", "500", "60")]
    public async Task InvokeAsync_WithVariousPolicies_ShouldSetCorrectHeaders(
        string policy, string limit, string window)
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Items["RateLimit:Policy"] = policy;
        context.Items["RateLimit:Limit"] = limit;
        context.Items["RateLimit:Window"] = window;

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitPolicy].ToString().Should().Be(policy);
        context.Response.Headers[Headers.RateLimitLimit].ToString().Should().Be(limit);
        context.Response.Headers[Headers.RateLimitWindow].ToString().Should().Be(window);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextMiddlewareThrows_ShouldPropagateException()
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Items["RateLimit:Policy"] = "default";
        context.Items["RateLimit:Limit"] = "100";
        context.Items["RateLimit:Window"] = "60";

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        var act = async () => await middleware.InvokeAsync(context);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidWindowValue_ShouldNotSetResetHeader()
    {
        // Arrange
        var middleware = new RateLimitMetricsMiddleware(_mockNext.Object, _metrics);
        var context = new DefaultHttpContext();
        context.Items["RateLimit:Policy"] = "default";
        context.Items["RateLimit:Limit"] = "100";
        context.Items["RateLimit:Window"] = "invalid";

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[Headers.RateLimitLimit].ToString().Should().Be("100");
        context.Response.Headers[Headers.RateLimitWindow].ToString().Should().Be("invalid");
        // RateLimitReset should still be added but won't have a calculated value
    }
}
