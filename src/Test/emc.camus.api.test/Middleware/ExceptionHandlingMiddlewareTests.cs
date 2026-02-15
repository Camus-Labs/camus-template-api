using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FluentAssertions;
using emc.camus.api.Middleware;
using emc.camus.api.Configurations;
using emc.camus.api.Metrics;
using emc.camus.application.Exceptions;
using emc.camus.application.Common;

namespace emc.camus.api.test.Middleware
{
    /// <summary>
    /// Tests for the ExceptionHandlingMiddleware class.
    /// </summary>
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<RequestDelegate> _nextMock;
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
        private readonly Mock<IHostEnvironment> _environmentMock;
        private readonly Mock<ErrorMetrics> _errorMetricsMock;
        private readonly DefaultHttpContext _httpContext;

        public ExceptionHandlingMiddlewareTests()
        {
            _nextMock = new Mock<RequestDelegate>();
            _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            _environmentMock = new Mock<IHostEnvironment>();
            _errorMetricsMock = new Mock<ErrorMetrics>("test-service", Mock.Of<ILogger<ErrorMetrics>>());
            _httpContext = new DefaultHttpContext();
            _httpContext.Response.Body = new MemoryStream();
        }

        private ExceptionHandlingMiddleware CreateMiddleware(bool isDevelopment = false)
        {
            _environmentMock.Setup(x => x.EnvironmentName).Returns(isDevelopment ? "Development" : "Production");
            
            // Create ErrorHandlingSettings with empty AdditionalRules (tests rely on PlatformRules)
            var settings = new ErrorHandlingSettings
            {
                AdditionalRules = new List<ErrorCodeMappingRule>()
            };
            var optionsWrapper = Options.Create(settings);
            
            return new ExceptionHandlingMiddleware(_nextMock.Object, _loggerMock.Object, _environmentMock.Object, optionsWrapper, _errorMetricsMock.Object);
        }

        [Fact]
        public async Task InvokeAsync_NoException_CallsNextMiddleware()
        {
            // Arrange
            var middleware = CreateMiddleware();
            _nextMock.Setup(next => next(_httpContext)).Returns(Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _nextMock.Verify(next => next(_httpContext), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ArgumentException_Development_ReturnsDetailedBadRequest()
        {
            // Arrange
            var middleware = CreateMiddleware(isDevelopment: true);
            var exception = new ArgumentException("Invalid argument");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, _httpContext.Response.StatusCode);
            Assert.Equal(MediaTypes.ProblemJson, _httpContext.Response.ContentType);

            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal((int)HttpStatusCode.BadRequest, problemDetails.Status);
            Assert.Equal("Bad Request", problemDetails.Title);
            Assert.Equal("Invalid argument", problemDetails.Detail);
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", problemDetails.Type);
            Assert.Contains("exceptionType", problemDetails.Extensions.Keys);
        }

        [Fact]
        public async Task InvokeAsync_ArgumentException_Production_ReturnsMinimalBadRequest()
        {
            // Arrange
            var middleware = CreateMiddleware(isDevelopment: false);
            var exception = new ArgumentException("Invalid argument");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, _httpContext.Response.StatusCode);
            Assert.Equal(MediaTypes.ProblemJson, _httpContext.Response.ContentType);

            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal((int)HttpStatusCode.BadRequest, problemDetails.Status);
            Assert.Equal("Bad Request", problemDetails.Title);
            Assert.Equal("Invalid argument", problemDetails.Detail); // Shows exception message for user-facing errors
            problemDetails.Extensions.Should().NotContainKey("exceptionType"); // No debug info in production
        }

        [Fact]
        public async Task InvokeAsync_UnauthorizedAccessException_ReturnsUnauthorized()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var exception = new UnauthorizedAccessException("Access denied");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.Unauthorized, _httpContext.Response.StatusCode);
            Assert.Equal(MediaTypes.ProblemJson, _httpContext.Response.ContentType);

            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal((int)HttpStatusCode.Unauthorized, problemDetails.Status);
            Assert.Equal("Unauthorized", problemDetails.Title);
            Assert.Equal("You are not authorized to access this resource.", problemDetails.Detail);
            Assert.Equal("https://tools.ietf.org/html/rfc7235#section-3.1", problemDetails.Type);
        }

        [Fact]
        public async Task InvokeAsync_GenericException_Development_ReturnsDetailedConflict()
        {
            // Arrange
            var middleware = CreateMiddleware(isDevelopment: true);
            var exception = new InvalidOperationException("Something went wrong");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.Conflict, _httpContext.Response.StatusCode);
            Assert.Equal(MediaTypes.ProblemJson, _httpContext.Response.ContentType);

            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal((int)HttpStatusCode.Conflict, problemDetails.Status);
            Assert.Equal("Conflict", problemDetails.Title);
            Assert.Equal("Something went wrong", problemDetails.Detail);
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.8", problemDetails.Type);
            Assert.Contains("exceptionType", problemDetails.Extensions.Keys);
        }

        [Fact]
        public async Task InvokeAsync_GenericException_Production_ReturnsMinimalConflict()
        {
            // Arrange
            var middleware = CreateMiddleware(isDevelopment: false);
            var exception = new InvalidOperationException("Something went wrong");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.Conflict, _httpContext.Response.StatusCode);
            Assert.Equal(MediaTypes.ProblemJson, _httpContext.Response.ContentType);

            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal((int)HttpStatusCode.Conflict, problemDetails.Status);
            Assert.Equal("Conflict", problemDetails.Title);
            Assert.Equal("Something went wrong", problemDetails.Detail);
            problemDetails.Extensions.Should().NotContainKey("exceptionType"); // No debug info
        }

        [Fact]
        public async Task InvokeAsync_ExceptionOccurs_LogsError()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var exception = new ArgumentException("Test exception");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception detected")),
                    It.Is<Exception>(ex => ex == exception),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_Exception_SetsInstancePath()
        {
            // Arrange
            var middleware = CreateMiddleware();
            _httpContext.Request.Path = "/api/test";
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(new Exception());

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.InternalServerError, _httpContext.Response.StatusCode);
            
            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal("/api/test", problemDetails.Instance);
        }

        [Fact]
        public async Task InvokeAsync_InvalidOperationExceptionWithPermission_ReturnsForbidden()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var exception = new InvalidOperationException("User does not have permission to perform this action");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.Forbidden, _httpContext.Response.StatusCode);
            Assert.Equal(MediaTypes.ProblemJson, _httpContext.Response.ContentType);

            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal((int)HttpStatusCode.Forbidden, problemDetails.Status);
            Assert.Equal("Forbidden", problemDetails.Title);
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.3", problemDetails.Type);
        }

        [Fact]
        public async Task InvokeAsync_InvalidOperationExceptionWithoutPermission_ReturnsConflict()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var exception = new InvalidOperationException("Invalid operation");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.Conflict, _httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_RateLimitExceededException_ReturnsTooManyRequestsWithRetryAfter()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var retryAfterSeconds = 45;
            var resetTimestamp = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds).ToUnixTimeSeconds();
            var exception = new RateLimitExceededException("strict", 100, 60, retryAfterSeconds, resetTimestamp);
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.TooManyRequests, _httpContext.Response.StatusCode);
            Assert.Equal(MediaTypes.ProblemJson, _httpContext.Response.ContentType);
            
            // Note: RetryAfter header is set by the rate limiting handler before throwing the exception,
            // not by the middleware. This test validates the middleware's response structure only.

            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal((int)HttpStatusCode.TooManyRequests, problemDetails.Status);
            Assert.Equal("Too Many Requests", problemDetails.Title);
            Assert.Contains("Rate limit exceeded", problemDetails.Detail);
            Assert.Equal("https://tools.ietf.org/html/rfc6585#section-4", problemDetails.Type);
            Assert.Contains("error", problemDetails.Extensions.Keys);
            Assert.Equal(ErrorCodes.RateLimitExceeded, problemDetails.Extensions["error"]?.ToString());
            Assert.Contains("retryAfter", problemDetails.Extensions.Keys);
            var retryAfterElement = (JsonElement)problemDetails.Extensions["retryAfter"]!;
            Assert.Equal(retryAfterSeconds, retryAfterElement.GetInt32());
        }

        [Fact]
        public async Task InvokeAsync_ExceptionWithErrorCodeInData_IncludesErrorCode()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var exception = new InvalidOperationException("Custom error");
            exception.Data[ErrorCodes.ErrorCodeKey] = "CUSTOM_ERROR_001";
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            problemDetails.Extensions.Should().ContainKey("error");
            Assert.Equal("CUSTOM_ERROR_001", problemDetails.Extensions["error"]?.ToString());
        }

        [Fact]
        public async Task InvokeAsync_ExceptionWithoutRetryAfter_DoesNotSetRetryAfterHeader()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var exception = new InvalidOperationException("Some error");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().NotContainKey(Headers.RetryAfter);

            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            problemDetails.Extensions.Should().NotContainKey("retryAfter");
        }
    }
}
