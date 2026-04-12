using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using emc.camus.api.Middleware;

namespace emc.camus.api.test.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private const string StrictCsp =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';";

    private const string SwaggerCsp =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';";

    private sealed class TrackingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _startingCallbacks = new();

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted { get; private set; }

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public void OnStarting(Func<object, Task> callback, object state)
            => _startingCallbacks.Add((callback, state));

        public async Task FireOnStartingAsync()
        {
            HasStarted = true;
            for (var i = _startingCallbacks.Count - 1; i >= 0; i--)
                await _startingCallbacks[i].Callback(_startingCallbacks[i].State);
        }
    }

    private static DefaultHttpContext CreateContextWithTracking(out TrackingResponseFeature responseFeature, string path = "/api/test")
    {
        responseFeature = new TrackingResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.Path = path;
        return context;
    }

    private static SecurityHeadersMiddleware CreateMiddleware(bool isDevelopment = false)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName)
            .Returns(isDevelopment ? "Development" : "Production");

        return new SecurityHeadersMiddleware(_ => Task.CompletedTask, environment.Object);
    }

    // --- Security Headers ---

    [Fact]
    public async Task InvokeAsync_AnyRequest_AddsXContentTypeOptions()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        var middleware = CreateMiddleware();

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.XContentTypeOptions].ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task InvokeAsync_AnyRequest_AddsXFrameOptions()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        var middleware = CreateMiddleware();

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.XFrameOptions].ToString().Should().Be("DENY");
    }

    [Fact]
    public async Task InvokeAsync_AnyRequest_AddsReferrerPolicy()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        var middleware = CreateMiddleware();

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task InvokeAsync_AnyRequest_AddsXXssProtection()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        var middleware = CreateMiddleware();

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.XXSSProtection].ToString().Should().Be("1; mode=block");
    }

    // --- Content Security Policy ---

    [Fact]
    public async Task InvokeAsync_ProductionNonSwaggerPath_AddsStrictCsp()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature, "/api/v1/info");
        var middleware = CreateMiddleware(isDevelopment: false);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.ContentSecurityPolicy].ToString().Should().Be(StrictCsp);
    }

    [Fact]
    public async Task InvokeAsync_ProductionSwaggerPath_AddsStrictCsp()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature, "/swagger/index.html");
        var middleware = CreateMiddleware(isDevelopment: false);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.ContentSecurityPolicy].ToString().Should().Be(StrictCsp);
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentSwaggerPath_AddsSwaggerCsp()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature, "/swagger/index.html");
        var middleware = CreateMiddleware(isDevelopment: true);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.ContentSecurityPolicy].ToString().Should().Be(SwaggerCsp);
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentNonSwaggerPath_AddsStrictCsp()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature, "/api/v1/info");
        var middleware = CreateMiddleware(isDevelopment: true);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.ContentSecurityPolicy].ToString().Should().Be(StrictCsp);
    }

    // --- Next Middleware Invocation ---

    [Fact]
    public async Task InvokeAsync_Always_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Production");

        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, environment.Object);

        var context = CreateContextWithTracking(out _);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }
}
