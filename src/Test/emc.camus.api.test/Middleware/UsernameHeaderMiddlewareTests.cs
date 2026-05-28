using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using emc.camus.api.Middleware;
using emc.camus.api.test.Helpers;
using emc.camus.application.Common;

namespace emc.camus.api.test.Middleware;

public class UsernameHeaderMiddlewareTests
{
    private static readonly System.Security.Claims.Claim[] AuthenticatedUserClaims =
        [new(System.Security.Claims.ClaimTypes.Name, "testuser")];
    private static DefaultHttpContext CreateContextWithTracking(out TrackingResponseFeature responseFeature)
    {
        responseFeature = new TrackingResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        return context;
    }

    // --- InvokeAsync ---

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_AddsUsernameHeader()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        var identity = new System.Security.Claims.ClaimsIdentity(AuthenticatedUserClaims, "TestAuth");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        var nextCalled = false;
        var middleware = new UsernameHeaderMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        nextCalled.Should().BeTrue();
        responseFeature.Headers[Headers.Username].ToString().Should().Be("testuser");
    }

    [Fact]
    public async Task InvokeAsync_AnonymousUser_AddsAnonymousHeader()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);

        var nextCalled = false;
        var middleware = new UsernameHeaderMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        nextCalled.Should().BeTrue();
        responseFeature.Headers[Headers.Username].ToString().Should().Be("anonymous");
    }

    public static readonly TheoryData<System.Security.Claims.ClaimsPrincipal> NonAuthenticatedUserScenarios = new()
    {
        new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity("TestAuth")),
        new System.Security.Claims.ClaimsPrincipal(),
        new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())
    };

    [Theory]
    [MemberData(nameof(NonAuthenticatedUserScenarios))]
    public async Task InvokeAsync_NonAuthenticatedUser_AddsAnonymousHeader(
        System.Security.Claims.ClaimsPrincipal user)
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        context.User = user;

        var middleware = new UsernameHeaderMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[Headers.Username].ToString().Should().Be("anonymous");
    }

    [Fact]
    public async Task InvokeAsync_UsernameHeaderAlreadyExists_OverwritesWithResolvedUsername()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        responseFeature.Headers[Headers.Username] = "existing-user";

        var middleware = new UsernameHeaderMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[Headers.Username].ToString().Should().Be("anonymous");
    }
}
