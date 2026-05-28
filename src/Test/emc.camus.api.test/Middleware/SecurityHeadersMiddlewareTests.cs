using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using emc.camus.api.Middleware;
using emc.camus.api.test.Helpers;

namespace emc.camus.api.test.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private const string SwaggerIndexPath = "/swagger/index.html";
    private const string ApiV1InfoPath = "/api/v1/info";
    private const string ProductionEnvironment = "Production";
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
            .Returns(isDevelopment ? "Development" : ProductionEnvironment);

        return new SecurityHeadersMiddleware(_ => Task.CompletedTask, environment.Object);
    }

    // --- Security Headers ---

    [Fact]
    public async Task InvokeAsync_AnyRequest_AddsAllSecurityHeaders()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        var middleware = CreateMiddleware();

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.XContentTypeOptions].ToString().Should().Be("nosniff");
        responseFeature.Headers[HeaderNames.XFrameOptions].ToString().Should().Be("DENY");
        responseFeature.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        responseFeature.Headers[HeaderNames.XXSSProtection].ToString().Should().Be("1; mode=block");
    }

    // --- Content Security Policy ---

    [Theory]
    [InlineData(ApiV1InfoPath, false)]
    [InlineData(SwaggerIndexPath, false)]
    [InlineData(ApiV1InfoPath, true)]
    public async Task InvokeAsync_NonSwaggerOrProductionPath_AddsStrictCsp(string path, bool isDevelopment)
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature, path);
        var middleware = CreateMiddleware(isDevelopment: isDevelopment);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.ContentSecurityPolicy].ToString().Should().Be(
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';");
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentSwaggerPath_AddsSwaggerCsp()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature, SwaggerIndexPath);
        var middleware = CreateMiddleware(isDevelopment: true);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[HeaderNames.ContentSecurityPolicy].ToString().Should().Be(
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';");
    }

    // --- Next Middleware Invocation ---

    [Fact]
    public async Task InvokeAsync_Always_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(ProductionEnvironment);

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
