using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using emc.camus.api.Middleware;
using emc.camus.application.Common;

namespace emc.camus.api.test.Middleware;

public class UsernameHeaderMiddlewareTests
{
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
        var claims = new System.Security.Claims.Claim[]
        {
            new(System.Security.Claims.ClaimTypes.Name, "testuser")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
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

    [Fact]
    public async Task InvokeAsync_AuthenticatedUserWithNullName_AddsAnonymousHeader()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        var identity = new System.Security.Claims.ClaimsIdentity("TestAuth");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        var middleware = new UsernameHeaderMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[Headers.Username].ToString().Should().Be("anonymous");
    }

    [Fact]
    public async Task InvokeAsync_NullUserIdentity_AddsAnonymousHeader()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        context.User = new System.Security.Claims.ClaimsPrincipal();

        var middleware = new UsernameHeaderMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        responseFeature.Headers[Headers.Username].ToString().Should().Be("anonymous");
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedIdentity_AddsAnonymousHeader()
    {
        // Arrange
        var context = CreateContextWithTracking(out var responseFeature);
        var identity = new System.Security.Claims.ClaimsIdentity(); // no auth type → IsAuthenticated = false
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

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
