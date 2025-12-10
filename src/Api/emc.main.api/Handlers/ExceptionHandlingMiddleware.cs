using System.Net;
using System.Text.Json;
using emc.camus.domain.Generic;

namespace emc.camus.main.api.Handlers
{
    /// <summary>
    /// /// ExceptionHandlingMiddleware is a middleware that handles exceptions thrown during the processing of HTTP requests.
    /// It catches exceptions, logs them, and returns a standardized error response to the client.
    /// It is typically used to centralize error handling in an ASP.NET Core application, ensuring that all unhandled exceptions are processed consistently.
    /// </summary> 
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// ExceptionHandlingMiddleware constructor initializes the middleware with a request delegate and a logger.
        /// This constructor is used to set up the middleware in the ASP.NET Core pipeline, allowing it to intercept requests and handle exceptions.
        /// It takes a RequestDelegate that represents the next middleware in the pipeline and an ILogger for logging errors.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="logger"></param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// /// InvokeAsync is the main method of the middleware that processes incoming HTTP requests.
        /// It is called for each request and is responsible for invoking the next middleware in the pipeline.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns> 
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
        /// It logs the exception and constructs a standardized error response in JSON format.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            int statusCode;
            string message;

            switch (exception)
            {
                case ArgumentException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = "Invalid request.";
                    break;
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    message = "You are not authorized to access this resource.";
                    break;
                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    message = "An unexpected error occurred.";
                    break;
            }

            _logger.LogError(exception, "An unhandled exception occurred: {ErrorMessage}", exception.Message);

            var response = new ErrorResponse
            {
                StatusCode = statusCode,
                Message = message
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = response.StatusCode;
            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }

    }
}