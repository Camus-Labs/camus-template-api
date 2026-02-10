using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Generic
{
    /// <summary>
    /// Standard error codes for API error responses.
    /// These codes provide machine-readable error identification for frontend error handling.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ErrorCodes
    {
        /// <summary>
        /// Error code for 400 Bad Request responses.
        /// Indicates the request contains invalid parameters or malformed data.
        /// </summary>
        public const string BadRequest = "bad_request";

        /// <summary>
        /// Error code for 401 Unauthorized responses when no authentication is provided.
        /// Indicates authentication is required to access the resource.
        /// </summary>
        public const string AuthenticationRequired = "authentication_required";

        /// <summary>
        /// Error code for 401 Unauthorized responses.
        /// General unauthorized access error.
        /// </summary>
        public const string Unauthorized = "unauthorized";

        /// <summary>
        /// Error code for 401 Unauthorized responses when credentials are provided but invalid.
        /// Indicates the provided credentials (API key, password, etc.) are incorrect.
        /// </summary>
        public const string InvalidCredentials = "invalid_credentials";

        /// <summary>
        /// Error code for 403 Forbidden responses.
        /// Indicates the authenticated user lacks permission to access the resource.
        /// </summary>
        public const string Forbidden = "forbidden";

        /// <summary>
        /// Error code for 429 Too Many Requests responses.
        /// Indicates the rate limit has been exceeded.
        /// </summary>
        public const string RateLimitExceeded = "rate_limit_exceeded";

        /// <summary>
        /// Error code for 500 Internal Server Error responses.
        /// Indicates an unexpected error occurred on the server.
        /// </summary>
        public const string InternalServerError = "internal_server_error";

        /// <summary>
        /// Fallback error code for unknown or unhandled errors.
        /// </summary>
        public const string UnknownError = "unknown_error";

        /// <summary>
        /// JWT-specific error codes for authentication failures.
        /// </summary>
        public static class Jwt
        {
            /// <summary>
            /// Error code when JWT token has expired.
            /// Frontend should refresh the token or redirect to login.
            /// </summary>
            public const string TokenExpired = "token_expired";

            /// <summary>
            /// Error code when JWT token is malformed or cannot be parsed.
            /// </summary>
            public const string InvalidToken = "invalid_token";

            /// <summary>
            /// Error code when JWT token signature validation fails.
            /// Indicates potential token tampering.
            /// </summary>
            public const string InvalidSignature = "invalid_signature";

            /// <summary>
            /// Error code when JWT token issuer does not match expected value.
            /// </summary>
            public const string InvalidIssuer = "invalid_issuer";

            /// <summary>
            /// Error code when JWT token audience does not match expected value.
            /// </summary>
            public const string InvalidAudience = "invalid_audience";
        }
    }
}
