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
}
