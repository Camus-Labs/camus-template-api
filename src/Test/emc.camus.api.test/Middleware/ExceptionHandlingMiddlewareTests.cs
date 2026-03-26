using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using emc.camus.api.Configurations;
using emc.camus.api.Metrics;
using emc.camus.api.Middleware;
using emc.camus.application.Common;
using emc.camus.application.Exceptions;

namespace emc.camus.api.test.Middleware;

public class ExceptionHandlingMiddlewareTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ErrorMetrics _errorMetrics;

    public ExceptionHandlingMiddlewareTests()
    {
        var logger = new Mock<ILogger<ErrorMetrics>>();
        _errorMetrics = new ErrorMetrics("test-service", logger.Object);
    }

    public void Dispose()
    {
        _errorMetrics.Dispose();
        GC.SuppressFinalize(this);
    }

    private ExceptionHandlingMiddleware CreateMiddleware(
        RequestDelegate next,
        bool isDevelopment = false,
        List<ErrorCodeMappingRule>? additionalRules = null)
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName)
            .Returns(isDevelopment ? "Development" : "Production");

        var settings = new ErrorHandlingSettings
        {
            AdditionalRules = additionalRules ?? new List<ErrorCodeMappingRule>()
        };
        var options = Options.Create(settings);

        return new ExceptionHandlingMiddleware(next, logger.Object, environment.Object, options, _errorMetrics);
    }

    private static async Task<ProblemDetails> GetProblemDetailsFromResponse(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions)!;
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    // --- ArgumentException ---

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400BadRequest()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentException("Invalid parameter value"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Be(MediaTypes.ProblemJson);

        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problemDetails.Detail.Should().Be("Invalid parameter value");
    }

    // --- KeyNotFoundException ---

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404NotFound()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new KeyNotFoundException("Resource not found"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    // --- UnauthorizedAccessException ---

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401Unauthorized()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new UnauthorizedAccessException("Access denied"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Status.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    // --- RateLimitExceededException ---

    [Fact]
    public async Task InvokeAsync_RateLimitExceededException_Returns429TooManyRequests()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new RateLimitExceededException("strict", 10, 60, 30, 1234567890));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);

        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Status.Should().Be((int)HttpStatusCode.TooManyRequests);
    }

    // --- InvalidOperationException with "not found" ---

    [Fact]
    public async Task InvokeAsync_InvalidOperationExceptionNotFound_Returns404()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Token not found in repository"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    // --- InvalidOperationException with "permission" ---

    [Fact]
    public async Task InvokeAsync_InvalidOperationExceptionPermission_Returns403Forbidden()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Insufficient permission to perform action"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
    }

    // --- InvalidOperationException (generic) ---

    [Fact]
    public async Task InvokeAsync_InvalidOperationExceptionGeneric_Returns409Conflict()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Duplicate entry detected"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }

    // --- Unhandled Exception ---

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500InternalServerError()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidProgramException("Something went wrong"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Status.Should().Be((int)HttpStatusCode.InternalServerError);
        problemDetails.Detail.Should().Be("An unexpected error occurred.");
    }

    // --- No Exception ---

    [Fact]
    public async Task InvokeAsync_NoException_DoesNotModifyResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }

    // --- Development Environment ---

    [Fact]
    public async Task InvokeAsync_DevelopmentEnvironment_IncludesExceptionDetails()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(
            _ => throw new ArgumentException("Bad value"),
            isDevelopment: true);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions.Should().ContainKey("exceptionType");
        problemDetails.Extensions.Should().ContainKey("exceptionMessage");
    }

    [Fact]
    public async Task InvokeAsync_ProductionEnvironment_ExcludesExceptionDetails()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(
            _ => throw new ArgumentException("Bad value"),
            isDevelopment: false);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions.Should().NotContainKey("exceptionType");
        problemDetails.Extensions.Should().NotContainKey("stackTrace");
    }

    // --- Error Code Resolution ---

    [Fact]
    public async Task InvokeAsync_JwtExpiredPattern_ResolvesJwtTokenExpiredErrorCode()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(
            _ => throw new UnauthorizedAccessException("JWT token expired at 2026-01-01"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions.Should().ContainKey("error");
        problemDetails.Extensions["error"]!.ToString().Should().Be(ErrorCodes.JwtTokenExpired);
    }

    [Fact]
    public async Task InvokeAsync_RateLimitException_ResolvesRateLimitErrorCode()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(
            _ => throw new RateLimitExceededException("strict", 10, 60, 30, 1234567890));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions.Should().ContainKey("error");
        problemDetails.Extensions["error"]!.ToString().Should().Be(ErrorCodes.RateLimitExceeded);
    }

    // --- ExplicitErrorCode in Exception.Data ---

    [Fact]
    public async Task InvokeAsync_ExplicitErrorCodeInExceptionData_UsesExplicitCode()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Some error");
        exception.Data[ErrorCodes.ErrorCodeKey] = "custom_error_code";

        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions.Should().ContainKey("error");
        problemDetails.Extensions["error"]!.ToString().Should().Be("custom_error_code");
    }

    // --- Additional (Configuration) Rules ---

    [Fact]
    public async Task InvokeAsync_AdditionalRuleMatchesFirst_OverridesPlatformRule()
    {
        // Arrange
        var context = CreateHttpContext();
        var additionalRules = new List<ErrorCodeMappingRule>
        {
            new() { Type = "ArgumentException", ErrorCode = "custom_bad_request" }
        };
        var middleware = CreateMiddleware(
            _ => throw new ArgumentException("test"),
            additionalRules: additionalRules);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions["error"]!.ToString().Should().Be("custom_bad_request");
    }

    // --- Inner Exception in Development ---

    [Fact]
    public async Task InvokeAsync_DevelopmentWithInnerException_IncludesInnerExceptionChain()
    {
        // Arrange
        var context = CreateHttpContext();
        var inner = new InvalidOperationException("inner error");
        var outer = new InvalidProgramException("outer error", inner);

        var middleware = CreateMiddleware(_ => throw outer, isDevelopment: true);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions.Should().ContainKey("innerExceptions");
    }

    // --- Request Path in Instance ---

    [Fact]
    public async Task InvokeAsync_SetsInstanceToRequestPath()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Request.Path = "/api/v2/auth/authenticate";
        var middleware = CreateMiddleware(_ => throw new InvalidProgramException("error"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Instance.Should().Be("/api/v2/auth/authenticate");
    }

    // --- Constructor Validation ---

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var environment = new Mock<IHostEnvironment>();
        var options = Options.Create(new ErrorHandlingSettings());

        // Act
        var act = () => new ExceptionHandlingMiddleware(next, null!, environment.Object, options, _errorMetrics);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullEnvironment_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var options = Options.Create(new ErrorHandlingSettings());

        // Act
        var act = () => new ExceptionHandlingMiddleware(next, logger.Object, null!, options, _errorMetrics);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var environment = new Mock<IHostEnvironment>();

        // Act
        var act = () => new ExceptionHandlingMiddleware(next, logger.Object, environment.Object, null!, _errorMetrics);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullErrorMetrics_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var environment = new Mock<IHostEnvironment>();
        var options = Options.Create(new ErrorHandlingSettings());

        // Act
        var act = () => new ExceptionHandlingMiddleware(next, logger.Object, environment.Object, options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
