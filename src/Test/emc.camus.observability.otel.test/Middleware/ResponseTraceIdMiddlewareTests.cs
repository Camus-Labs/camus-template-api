using System.Diagnostics;
using emc.camus.application.Common;
using emc.camus.observability.otel.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace emc.camus.observability.otel.test.Middleware;

/// <summary>
/// Unit tests for ResponseTraceIdMiddleware to verify trace ID header functionality.
/// Uses TestServer to properly test OnStarting callbacks in the HTTP pipeline.
/// </summary>
public class ResponseTraceIdMiddlewareTests
{
    private async Task<IHost> CreateTestHost(Action<IApplicationBuilder> configureMiddleware)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(configureMiddleware);
            })
            .StartAsync();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddTraceIdHeader()
    {
        // Arrange
        Activity.Current = null;
        
        using var host = await CreateTestHost(app =>
        {
            app.UseMiddleware<ResponseTraceIdMiddleware>();
            app.Run(async context => await context.Response.WriteAsync("OK"));
        });

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.Should().ContainKey(Headers.TraceId);
        response.Headers.GetValues(Headers.TraceId).First().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        
        using var host = await CreateTestHost(app =>
        {
            app.UseMiddleware<ResponseTraceIdMiddleware>();
            app.Run(context =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
        });

        var client = host.GetTestClient();

        // Act
        await client.GetAsync("/");

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ShouldPropagateException()
    {
        // Arrange
        using var host = await CreateTestHost(app =>
        {
            app.UseMiddleware<ResponseTraceIdMiddleware>();
            app.Run(context => throw new InvalidOperationException("Test exception"));
        });

        var client = host.GetTestClient();

        // Act & Assert
        var act = async () => await client.GetAsync("/");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public void Constructor_ShouldAcceptRequestDelegate()
    {
        // Arrange & Act
        RequestDelegate next = ctx => Task.CompletedTask;
        var middleware = new ResponseTraceIdMiddleware(next);

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderAlreadyExists_ShouldNotOverwrite()
    {
        // Arrange
        var existingTraceId = "existing-trace-id";

        using var host = await CreateTestHost(app =>
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers[Headers.TraceId] = existingTraceId;
                await next();
            });
            app.UseMiddleware<ResponseTraceIdMiddleware>();
            app.Run(async context => await context.Response.WriteAsync("OK"));
        });

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.GetValues(Headers.TraceId).First().Should().Be(existingTraceId);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyTraceId_ShouldNotAddHeader()
    {
        // Arrange
        Activity.Current = null;

        using var host = await CreateTestHost(app =>
        {
            app.Use(async (context, next) =>
            {
                context.TraceIdentifier = "";
                await next();
            });
            app.UseMiddleware<ResponseTraceIdMiddleware>();
            app.Run(async context => await context.Response.WriteAsync("OK"));
        });

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.Should().NotContainKey(Headers.TraceId);
    }

    [Fact]
    public async Task InvokeAsync_WithWhitespaceTraceId_ShouldNotAddHeader()
    {
        // Arrange
        Activity.Current = null;

        using var host = await CreateTestHost(app =>
        {
            app.Use(async (context, next) =>
            {
                context.TraceIdentifier = "   ";
                await next();
            });
            app.UseMiddleware<ResponseTraceIdMiddleware>();
            app.Run(async context => await context.Response.WriteAsync("OK"));
        });

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.Should().NotContainKey(Headers.TraceId);
    }

    [Fact]
    public async Task InvokeAsync_WithActiveActivity_ShouldUseActivityTraceId()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "TestSource",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("TestActivity");
        var expectedTraceId = activity!.TraceId.ToString();

        using var host = await CreateTestHost(app =>
        {
            app.Use(async (context, next) =>
            {
                Activity.Current = activity;
                await next();
            });
            app.UseMiddleware<ResponseTraceIdMiddleware>();
            app.Run(async context => await context.Response.WriteAsync("OK"));
        });

        var client = host.GetTestClient();

        try
        {
            // Act
            var response = await client.GetAsync("/");

            // Assert
            response.Headers.Should().ContainKey(Headers.TraceId);
            var traceId = response.Headers.GetValues(Headers.TraceId).First();
            traceId.Should().Be(expectedTraceId);
        }
        finally
        {
            Activity.Current = null;
            activity?.Dispose();
            activitySource.Dispose();
        }
    }
}

