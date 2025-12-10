using System.Net;
using System.Text.Json;
using emc.camus.domain.Generic;
using Microsoft.AspNetCore.Diagnostics;

namespace emc.camus.main.api.Handlers
{
    /// <summary>
    /// GlobalExceptionHandler provides a static method to handle exceptions for UseExceptionHandler middleware.
    /// </summary>
    public static class GlobalExceptionHandler
    {
        /// <summary>
        /// Handles exceptions for UseExceptionHandler, returning a standardized error response.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public static async Task HandleAsync(HttpContext context)
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;
            int statusCode;
            string message;

            switch (exception)
            {
                case BadHttpRequestException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = "Malformed request body or invalid JSON.";
                    break;
                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    message = "A global error occurred before processing the request .";
                    break;
            }

            var loggerFactory = context.RequestServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            var logger = loggerFactory?.CreateLogger("GlobalExceptionHandler");
            logger?.LogError(exception, "Global error handler caught: {ErrorMessage}", exception?.Message);

            var errorResponse = new ErrorResponse
            {
                StatusCode = statusCode,
                Message = message
            };
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
