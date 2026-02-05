using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace emc.camus.main.api.Handlers
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
            
            ProblemDetails problemDetails = exception switch
            {
                ArgumentException argEx => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Bad Request",
                    Detail = isDevelopment ? $"Argument validation failed: {argEx.Message}" : "The request contains invalid parameters.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                },
                UnauthorizedAccessException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = "Unauthorized",
                    Detail = isDevelopment ? exception.Message : "You are not authorized to access this resource.",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
                },
                InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("permission") => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Title = "Forbidden",
                    Detail = isDevelopment ? invalidOpEx.Message : "You do not have permission to access this resource.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
                },
                _ => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = isDevelopment ? $"An unexpected error occurred: {exception.Message}" : "An unexpected error occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                }
            };

            // Add request path as instance identifier
            problemDetails.Instance = context.Request.Path;
            
            // Add additional development debugging information
            if (isDevelopment)
            {
                problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                }
            }

            _logger.LogError(exception, "Exception detected: {ErrorMessage}", exception.Message);

            context.Response.StatusCode = problemDetails.Status ?? 500;
            context.Response.ContentType = "application/problem+json";
            
            // Manually serialize to maintain control over ContentType
            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            
            return context.Response.WriteAsync(json);
        }

    }
}
