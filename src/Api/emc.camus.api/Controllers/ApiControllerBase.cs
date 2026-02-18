using Microsoft.AspNetCore.Mvc;
using emc.camus.api.Models.Responses;

namespace emc.camus.api.Controllers;

/// <summary>
/// Base controller providing standardized response helpers for all API controllers.
/// </summary>
/// <remarks>
/// This base controller ensures consistent response formatting across all endpoints:
/// - Success responses are wrapped in ApiResponse&lt;T&gt;
/// - Error responses use ProblemDetails (handled by ExceptionHandlingMiddleware)
/// - Timestamps are automatically set to UTC
/// 
/// All API controllers should inherit from this class to ensure standardization.
/// </remarks>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Returns a successful response (200 OK) with data wrapped in ApiResponse.
    /// </summary>
    /// <typeparam name="T">Type of the response data</typeparam>
    /// <param name="data">The data to return</param>
    /// <param name="message">Success message describing the operation</param>
    /// <returns>200 OK with standardized ApiResponse wrapper</returns>
    protected IActionResult Success<T>(T data, string message)
    {
        return Ok(new ApiResponse<T>
        {
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Returns a created response (201 Created) with data wrapped in ApiResponse.
    /// </summary>
    /// <typeparam name="T">Type of the response data</typeparam>
    /// <param name="data">The created resource data</param>
    /// <param name="message">Success message describing the creation</param>
    /// <param name="actionName">Name of the action to retrieve the created resource</param>
    /// <param name="routeValues">Route values for generating the location header</param>
    /// <returns>201 Created with Location header and standardized ApiResponse wrapper</returns>
    protected IActionResult Created<T>(T data, string message, string? actionName = null, object? routeValues = null)
    {
        var response = new ApiResponse<T>
        {
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        return CreatedAtAction(actionName, routeValues, response);
    }

    /// <summary>
    /// Returns an accepted response (202 Accepted) with data wrapped in ApiResponse.
    /// Useful for long-running operations that are accepted but not completed yet.
    /// </summary>
    /// <typeparam name="T">Type of the response data</typeparam>
    /// <param name="data">The data to return (e.g., operation ID)</param>
    /// <param name="message">Message describing the accepted operation</param>
    /// <returns>202 Accepted with standardized ApiResponse wrapper</returns>
    protected IActionResult Accepted<T>(T data, string message)
    {
        return base.Accepted(new ApiResponse<T>
        {
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Returns a no content response (204 No Content) for successful operations with no data.
    /// </summary>
    /// <returns>204 No Content</returns>
    protected IActionResult NoContentSuccess()
    {
        return NoContent();
    }
}
