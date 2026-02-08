
namespace emc.camus.domain.Generic
{
    /// <summary>
    /// Represents a standard API response envelope for successful operations with strongly-typed data.
    /// Use HTTP status codes (200 OK, 201 Created, etc.) to indicate success; use ProblemDetails for errors.
    /// </summary>
    /// <typeparam name="T">The type of the data payload (e.g., ApiInfo, AuthToken, Credentials, etc.)</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Gets or sets the main message for the response (e.g., "Token generated successfully").
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the strongly-typed data payload for the response.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this response was generated (UTC).
        /// Useful for caching, debugging, and client-side temporal logic.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}