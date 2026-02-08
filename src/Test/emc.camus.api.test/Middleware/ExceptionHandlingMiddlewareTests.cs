using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using emc.camus.api.Middleware;

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
        private readonly DefaultHttpContext _httpContext;

        public ExceptionHandlingMiddlewareTests()
        {
            _nextMock = new Mock<RequestDelegate>();
            _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            _environmentMock = new Mock<IHostEnvironment>();
            _httpContext = new DefaultHttpContext();
            _httpContext.Response.Body = new MemoryStream();
        }

        private ExceptionHandlingMiddleware CreateMiddleware(bool isDevelopment = false)
        {
            _environmentMock.Setup(x => x.EnvironmentName).Returns(isDevelopment ? "Development" : "Production");
            return new ExceptionHandlingMiddleware(_nextMock.Object, _loggerMock.Object, _environmentMock.Object);
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
            Assert.Equal("application/problem+json", _httpContext.Response.ContentType);

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
            Assert.Contains("Argument validation failed", problemDetails.Detail);
            Assert.Contains("Invalid argument", problemDetails.Detail);
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
            Assert.Equal("application/problem+json", _httpContext.Response.ContentType);

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
            Assert.Equal("The request contains invalid parameters.", problemDetails.Detail);
            Assert.DoesNotContain("Invalid argument", problemDetails.Detail); // No sensitive info in production
            Assert.False(problemDetails.Extensions.ContainsKey("exceptionType")); // No debug info in production
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
            Assert.Equal("application/problem+json", _httpContext.Response.ContentType);

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
        public async Task InvokeAsync_GenericException_Development_ReturnsDetailedInternalServerError()
        {
            // Arrange
            var middleware = CreateMiddleware(isDevelopment: true);
            var exception = new InvalidOperationException("Something went wrong");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.InternalServerError, _httpContext.Response.StatusCode);
            Assert.Equal("application/problem+json", _httpContext.Response.ContentType);

            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problemDetails.Status);
            Assert.Equal("Internal Server Error", problemDetails.Title);
            Assert.Contains("An unexpected error occurred", problemDetails.Detail);
            Assert.Contains("Something went wrong", problemDetails.Detail);
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.1", problemDetails.Type);
            Assert.Contains("exceptionType", problemDetails.Extensions.Keys);
        }

        [Fact]
        public async Task InvokeAsync_GenericException_Production_ReturnsMinimalInternalServerError()
        {
            // Arrange
            var middleware = CreateMiddleware(isDevelopment: false);
            var exception = new InvalidOperationException("Something went wrong");
            _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal((int)HttpStatusCode.InternalServerError, _httpContext.Response.StatusCode);
            Assert.Equal("application/problem+json", _httpContext.Response.ContentType);

            // Read response body
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(problemDetails);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problemDetails.Status);
            Assert.Equal("Internal Server Error", problemDetails.Title);
            Assert.Equal("An unexpected error occurred.", problemDetails.Detail);
            Assert.DoesNotContain("Something went wrong", problemDetails.Detail); // No sensitive info
            Assert.False(problemDetails.Extensions.ContainsKey("exceptionType")); // No debug info
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
    }
}
