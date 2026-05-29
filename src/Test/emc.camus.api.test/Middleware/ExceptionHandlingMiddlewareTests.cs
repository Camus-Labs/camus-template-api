using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using emc.camus.api.Configurations;
using emc.camus.api.Metrics;
using emc.camus.api.Middleware;
using emc.camus.api.test.Helpers;
using emc.camus.api.Exceptions;
using emc.camus.application.Common;
using emc.camus.application.Exceptions;
using emc.camus.domain.Exceptions;

namespace emc.camus.api.test.Middleware;

public class ExceptionHandlingMiddlewareTests : IDisposable
{
    private const string ServiceName = "test-service";
    private const string PathTagKey = "path";
    private const string AuthenticatePath = "/api/v2/auth/authenticate";
    private const string TestRoutePattern = "/api/v1/test/{id}";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<ErrorCodeMappingRuleSettings> CustomProgramErrorRules =
    [
        new() { Type = "InvalidProgramException", ErrorCode = "custom_program_error" }
    ];

    private static readonly List<ErrorCodeMappingRuleSettings> RegexTimeoutRules =
    [
        new() { Type = "InvalidProgramException", Pattern = @"^(a+)+$", ErrorCode = "should_not_match" }
    ];

    private static readonly List<ErrorCodeMappingRuleSettings> EmptyRules = [];

    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private readonly ConcurrentBag<(LogLevel Level, string Message)> _logEntries;
    private readonly ErrorMetrics _errorMetrics;

    public ExceptionHandlingMiddlewareTests()
    {
        (_loggerMock, _logEntries) = LogCaptureBuilder.Create<ExceptionHandlingMiddleware>();
        var metricsLogger = new Mock<ILogger<ErrorMetrics>>();
        _errorMetrics = new ErrorMetrics(ServiceName, metricsLogger.Object);
    }

    public void Dispose()
    {
        _errorMetrics.Dispose();
        GC.SuppressFinalize(this);
    }

    private ExceptionHandlingMiddleware CreateMiddleware(
        RequestDelegate next,
        bool isDevelopment = false,
        List<ErrorCodeMappingRuleSettings>? additionalRules = null,
        ErrorMetrics? errorMetrics = null)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName)
            .Returns(isDevelopment ? "Development" : "Production");

        var settings = new ErrorHandlingSettings
        {
            AdditionalRules = additionalRules ?? EmptyRules
        };

        return new ExceptionHandlingMiddleware(next, _loggerMock.Object, environment.Object, settings, errorMetrics ?? _errorMetrics);
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

    public static readonly TheoryData<Exception, HttpStatusCode> ExceptionToStatusCodeMappings = new()
    {
        { new KeyNotFoundException("Resource not found"), HttpStatusCode.NotFound },
        { new UnauthorizedAccessException("Access denied"), HttpStatusCode.Unauthorized },
        { new RateLimitExceededException("strict", 10, 60, 30, 1234567890), HttpStatusCode.TooManyRequests },
        { new DataConflictException("A generated token with username 'Admin-test' already exists."), HttpStatusCode.Conflict },
        { new DomainException("Permissions not a subset of creator's permissions."), HttpStatusCode.UnprocessableEntity },
        { new OperationCanceledException("The operation was canceled."), HttpStatusCode.GatewayTimeout }
    };

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
    [InlineData("You do not have permission to access this resource. User: test-user", HttpStatusCode.Forbidden)]
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

    public static readonly TheoryData<Exception, string> ExceptionToErrorCodeMappings = new()
    {
        { new UnauthorizedAccessException("JWT token expired at 2026-01-01"), ErrorCodes.JwtTokenExpired },
        { new RateLimitExceededException("strict", 10, 60, 30, 1234567890), ErrorCodes.RateLimitExceeded },
        { new DomainException("Permissions not a subset of creator's permissions."), ErrorCodes.DomainRuleViolation }
    };

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

    // --- Additional (Configuration) Rules ---

    [Fact]
    public async Task InvokeAsync_AdditionalRuleFallback_MatchesWhenNoPlatformRuleApplies()
    {
        // Arrange — InvalidProgramException has no platform rule, so additional rule matches
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(
            _ => throw new InvalidProgramException("test"),
            additionalRules: CustomProgramErrorRules);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions["error"]!.ToString().Should().Be("custom_program_error");
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
        context.Request.Path = AuthenticatePath;
        var middleware = CreateMiddleware(_ => throw new InvalidProgramException("error"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Instance.Should().Be(AuthenticatePath);
    }

    // --- Regex Timeout in Rule ---

    [Fact]
    public async Task InvokeAsync_RegexPatternTimesOut_SkipsRuleAndFallsThrough()
    {
        // Arrange
        var context = CreateHttpContext();
        // Input designed to cause catastrophic backtracking on the pattern above
        var message = new string('a', 50) + "!";
        var middleware = CreateMiddleware(
            _ => throw new InvalidProgramException(message),
            additionalRules: RegexTimeoutRules);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var problemDetails = await GetProblemDetailsFromResponse(context);
        problemDetails.Extensions["error"]!.ToString().Should().Be(ErrorCodes.DefaultErrorCode);
    }

    // --- Route Pattern Resolution ---

    [Fact]
    public async Task InvokeAsync_NoEndpoint_RecordsUnresolvedInMetrics()
    {
        // Arrange
        string? recordedPath = null;
        var metricsLogger = new Mock<ILogger<ErrorMetrics>>();
        using var errorMetrics = new ErrorMetrics("route-test-unresolved", metricsLogger.Object);
        using var listener = CreateMetricsListener("route-test-unresolved", tags => recordedPath = GetTagValue(tags, PathTagKey));

        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentException("test"), errorMetrics: errorMetrics);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        recordedPath.Should().Be("unresolved");
    }

    [Fact]
    public async Task InvokeAsync_RouteEndpointPresent_RecordsRoutePatternInMetrics()
    {
        // Arrange
        string? recordedPath = null;
        var metricsLogger = new Mock<ILogger<ErrorMetrics>>();
        using var errorMetrics = new ErrorMetrics("route-test-resolved", metricsLogger.Object);
        using var listener = CreateMetricsListener("route-test-resolved", tags => recordedPath = GetTagValue(tags, PathTagKey));

        var context = CreateHttpContext();
        var routePattern = RoutePatternFactory.Parse(TestRoutePattern);
        var endpoint = new RouteEndpoint(
            _ => Task.CompletedTask,
            routePattern,
            order: 0,
            metadata: new EndpointMetadataCollection(),
            displayName: "TestEndpoint");
        context.SetEndpoint(endpoint);

        var middleware = CreateMiddleware(_ => throw new ArgumentException("test"), errorMetrics: errorMetrics);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        recordedPath.Should().Be(TestRoutePattern);
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == key)
                return tag.Value?.ToString();
        }
        return null;
    }

    private static MeterListener CreateMetricsListener(
        string meterNamePrefix,
        Action<ReadOnlySpan<KeyValuePair<string, object?>>> onMeasurement)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "error_responses_total" &&
                instrument.Meter.Name.StartsWith(meterNamePrefix, StringComparison.Ordinal))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => onMeasurement(tags));
        listener.Start();
        return listener;
    }

    // --- Constructor Validation ---

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var environment = new Mock<IHostEnvironment>().Object;
        var settings = new ErrorHandlingSettings();
        using var errorMetrics = new ErrorMetrics(ServiceName, new Mock<ILogger<ErrorMetrics>>().Object);

        // Act
        var act = () => new ExceptionHandlingMiddleware(next, null!, environment, settings, errorMetrics);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullEnvironment_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>().Object;
        var settings = new ErrorHandlingSettings();
        using var errorMetrics = new ErrorMetrics(ServiceName, new Mock<ILogger<ErrorMetrics>>().Object);

        // Act
        var act = () => new ExceptionHandlingMiddleware(next, logger, null!, settings, errorMetrics);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>().Object;
        var environment = new Mock<IHostEnvironment>().Object;
        using var errorMetrics = new ErrorMetrics(ServiceName, new Mock<ILogger<ErrorMetrics>>().Object);

        // Act
        var act = () => new ExceptionHandlingMiddleware(next, logger, environment, null!, errorMetrics);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullErrorMetrics_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>().Object;
        var environment = new Mock<IHostEnvironment>().Object;
        var settings = new ErrorHandlingSettings();

        // Act
        var act = () => new ExceptionHandlingMiddleware(next, logger, environment, settings, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
