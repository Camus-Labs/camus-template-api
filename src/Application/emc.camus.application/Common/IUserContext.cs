namespace emc.camus.application.Common;

/// <summary>
/// Provides access to the current user context for auditing and authorization.
/// This abstraction allows the application layer to access user information
/// without depending on HTTP-specific implementations.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the unique identifier of the current authenticated user.
    /// </summary>
    /// <returns>The user ID if authenticated, otherwise null.</returns>
    Guid? GetCurrentUserId();

    /// <summary>
    /// Gets the username of the current authenticated user.
    /// </summary>
    /// <returns>The username if authenticated, otherwise null.</returns>
    string? GetCurrentUsername();

    /// <summary>
    /// Gets the current trace ID from OpenTelemetry Activity context.
    /// </summary>
    /// <returns>The trace ID if available, otherwise null.</returns>
    string? GetCurrentTraceId();
}
