using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using emc.camus.application.Generic;
using emc.camus.application.Exceptions;

namespace emc.camus.api.Middleware
{
    /// <summary>
    /// ExceptionHandlingMiddleware is a middleware that handles exceptions thrown during the processing of HTTP requests.
    /// It catches exceptions, logs them, and returns a standardized RFC 7807 ProblemDetails response to the client.
    /// The middleware provides environment-specific detail levels - verbose in Development, minimal in Production.
    /// It is typically used to centralize error handling in an ASP.NET Core application, ensuring that all unhandled exceptions are processed consistently.
    /// </summary> 
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        /// <summary>
        /// ExceptionHandlingMiddleware constructor initializes the middleware with a request delegate, logger, and host environment.
        /// This constructor is used to set up the middleware in the ASP.NET Core pipeline, allowing it to intercept requests and handle exceptions.
        /// The host environment is used to determine detail level for error responses (verbose in Development, minimal in Production).
        /// </summary>
        /// <param name="next">The next middleware in the pipeline to invoke after processing</param>
        /// <param name="logger">Logger instance for recording exception information</param>
        /// <param name="environment">Host environment to determine error detail level</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// InvokeAsync is the main method of the middleware that processes incoming HTTP requests.
        /// It is called for each request and is responsible for invoking the next middleware in the pipeline.
        /// If an exception occurs during request processing, it catches the exception and handles it gracefully.
        /// </summary>
        /// <param name="context">The HTTP context for the current request</param>
        /// <returns>A task representing the asynchronous operation</returns> 
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// HandleExceptionAsync is a method that handles exceptions thrown during the processing of HTTP requests.
        /// It determines the appropriate HTTP status code and message based on the type of exception.
        /// It logs the exception and constructs a standardized RFC 7807 ProblemDetails response.
        /// In Development environment, includes detailed exception information. In Production, provides minimal details.
        /// </summary>
        /// <param name="context">The HTTP context for the current request</param>
        /// <param name="exception">The exception that was thrown during request processing</param>
        /// <returns>A task representing the asynchronous operation of writing the ProblemDetails response</returns>
        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var isDevelopment = _environment.IsDevelopment();
            var problemDetails = CreateProblemDetails(exception, isDevelopment);
            
            problemDetails.Instance = context.Request.Path;
            
            AddErrorCode(problemDetails, exception);
            AddRetryAfterMetadata(problemDetails, exception, context);
            AddDevelopmentInformation(problemDetails, exception, isDevelopment);

            _logger.LogError(exception, "Exception detected: {ErrorMessage}", exception.Message);

            context.Response.StatusCode = problemDetails.Status ?? 500;
            context.Response.ContentType = MediaTypes.ProblemJson;
            
            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            
            return context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Creates a ProblemDetails object based on the exception type.
        /// </summary>
        private ProblemDetails CreateProblemDetails(Exception exception, bool isDevelopment)
        {
            return exception switch
            {
                ArgumentException argEx => CreateBadRequestProblem(argEx, isDevelopment),
                UnauthorizedAccessException => CreateUnauthorizedProblem(exception, isDevelopment),
                RateLimitExceededException => CreateRateLimitProblem(exception),
                InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("permission") => CreateForbiddenProblem(invalidOpEx, isDevelopment),
                _ => CreateInternalServerErrorProblem(exception, isDevelopment)
            };
        }

        private ProblemDetails CreateBadRequestProblem(ArgumentException exception, bool isDevelopment)
        {
            return new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Bad Request",
                Detail = isDevelopment ? $"Argument validation failed: {exception.Message}" : "The request contains invalid parameters.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            };
        }

        private ProblemDetails CreateUnauthorizedProblem(Exception exception, bool isDevelopment)
        {
            return new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = isDevelopment ? exception.Message : "You are not authorized to access this resource.",
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
            };
        }

        private ProblemDetails CreateRateLimitProblem(Exception exception)
        {
            return new ProblemDetails
            {
                Status = (int)HttpStatusCode.TooManyRequests,
                Title = "Too Many Requests",
                Detail = exception.Message,
                Type = "https://tools.ietf.org/html/rfc6585#section-4",
                Extensions =
                {
                    ["error"] = ErrorCodes.RateLimitExceeded
                }
            };
        }

        private ProblemDetails CreateForbiddenProblem(InvalidOperationException exception, bool isDevelopment)
        {
            return new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Forbidden",
                Detail = isDevelopment ? exception.Message : "You do not have permission to access this resource.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
            };
        }

        private ProblemDetails CreateInternalServerErrorProblem(Exception exception, bool isDevelopment)
        {
            return new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = isDevelopment ? $"An unexpected error occurred: {exception.Message}" : "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
        }

        /// <summary>
        /// Adds machine-readable error code to the problem details.
        /// </summary>
        private void AddErrorCode(ProblemDetails problemDetails, Exception exception)
        {
            if (exception.Data.Contains(ErrorCodes.ErrorCodeKey))
            {
                problemDetails.Extensions["error"] = exception.Data[ErrorCodes.ErrorCodeKey];
            }
            else
            {
                problemDetails.Extensions["error"] = ErrorCodes.GetErrorCodeFromStatusCode(problemDetails.Status ?? 500);
            }
        }

        /// <summary>
        /// Adds RetryAfter metadata if present in the exception data.
        /// </summary>
        private void AddRetryAfterMetadata(ProblemDetails problemDetails, Exception exception, HttpContext context)
        {
            int? retryAfterSeconds = null;

            // Check if it's a RateLimitExceededException
            if (exception is RateLimitExceededException rateLimitException)
            {
                retryAfterSeconds = rateLimitException.RetryAfterSeconds;
            }

            if (retryAfterSeconds.HasValue)
            {
                problemDetails.Extensions["retryAfter"] = retryAfterSeconds.Value;
                context.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString();
            }
        }

        /// <summary>
        /// Adds development debugging information to the problem details.
        /// </summary>
        private void AddDevelopmentInformation(ProblemDetails problemDetails, Exception exception, bool isDevelopment)
        {
            if (isDevelopment)
            {
                problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
                if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                {
                    problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                }
            }
        }

    }
}
