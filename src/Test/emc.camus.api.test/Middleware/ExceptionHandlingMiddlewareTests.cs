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

    // --- Exception-to-StatusCode mapping ---

    public static IEnumerable<object[]> ExceptionToStatusCodeMappings()
    {
        yield return new object[] { new KeyNotFoundException("Resource not found"), HttpStatusCode.NotFound };
        yield return new object[] { new UnauthorizedAccessException("Access denied"), HttpStatusCode.Unauthorized };
        yield return new object[] { new RateLimitExceededException("strict", 10, 60, 30, 1234567890), HttpStatusCode.TooManyRequests };
        yield return new object[] { new DataConflictException("A generated token with username 'Admin-test' already exists."), HttpStatusCode.Conflict };
    }

    [Theory]
    [MemberData(nameof(ExceptionToStatusCodeMappings))]
    public async Task InvokeAsync_MappedException_ReturnsExpectedStatusCode(
        Exception exception, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)expectedStatusCode);

        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Status.Should().Be((int)expectedStatusCode);
    }

    // --- InvalidOperationException mapped by message ---

    [Theory]
    [InlineData("Token not found in repository", HttpStatusCode.NotFound)]
    [InlineData("Insufficient permission to perform action", HttpStatusCode.Forbidden)]
    [InlineData("Invalid Setting", HttpStatusCode.InternalServerError)]
    public async Task InvokeAsync_InvalidOperationException_ReturnsExpectedStatusCode(
        string message, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(message));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)expectedStatusCode);
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

    public static IEnumerable<object[]> ExceptionToErrorCodeMappings()
    {
        yield return new object[]
        {
            new UnauthorizedAccessException("JWT token expired at 2026-01-01"),
            ErrorCodes.JwtTokenExpired
        };
        yield return new object[]
        {
            new RateLimitExceededException("strict", 10, 60, 30, 1234567890),
            ErrorCodes.RateLimitExceeded
        };
    }

    [Theory]
    [MemberData(nameof(ExceptionToErrorCodeMappings))]
    public async Task InvokeAsync_MappedException_ResolvesExpectedErrorCode(
        Exception exception, string expectedErrorCode)
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions.Should().ContainKey("error");
        problemDetails.Extensions["error"]!.ToString().Should().Be(expectedErrorCode);
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

    public static IEnumerable<object?[]> Constructor_NullDependencyScenarios()
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>().Object;
        var environment = new Mock<IHostEnvironment>().Object;
        var options = Options.Create(new ErrorHandlingSettings());
        var metricsLogger = new Mock<ILogger<ErrorMetrics>>();
        var errorMetrics = new ErrorMetrics("test-service", metricsLogger.Object);

        yield return new object?[] { null, environment, options, errorMetrics };
        yield return new object?[] { logger, null, options, errorMetrics };
        yield return new object?[] { logger, environment, null, errorMetrics };
        yield return new object?[] { logger, environment, options, null };
    }

    [Theory]
    [MemberData(nameof(Constructor_NullDependencyScenarios))]
    public void Constructor_NullDependency_ThrowsArgumentNullException(
        ILogger<ExceptionHandlingMiddleware>? logger,
        IHostEnvironment? environment,
        IOptions<ErrorHandlingSettings>? options,
        ErrorMetrics? errorMetrics)
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Act
        var act = () => new ExceptionHandlingMiddleware(next, logger!, environment!, options!, errorMetrics!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
