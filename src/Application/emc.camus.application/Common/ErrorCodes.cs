using System.Diagnostics.CodeAnalysis;

namespace emc.camus.application.Common;

/// <summary>
/// Standard error codes for API error responses.
/// These codes provide machine-readable error identification for frontend error handling.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ErrorCodes
    {
        /// <summary>
        /// Dictionary key for storing explicit error codes in exception.Data (deprecated pattern).
        /// This pattern is discouraged - error codes should be automatically detected via configuration.
        /// </summary>
        public const string ErrorCodeKey = "ErrorCode";
        
        /// <summary>
        /// Default error code returned when no rules match an exception.
        /// Indicates an unhandled or unexpected error type.
        /// </summary>
        public const string DefaultErrorCode = InternalServerError;

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
        /// Error code for 401 Unauthorized responses when API Key is missing.
        /// </summary>
        public const string ApiKeyAuthenticationRequired = "apikey_authentication_required";

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
        /// Error code for 401 Unauthorized responses when username/password authentication fails.
        /// </summary>
        public const string AuthInvalidCredentials = "auth_invalid_credentials";

        /// <summary>
        /// Error code for 401 Unauthorized responses when API Key is invalid.
        /// </summary>
        public const string ApiKeyInvalidCredentials = "apikey_invalid_credentials";

        /// <summary>
        /// Error code for 403 Forbidden responses.
        /// Indicates the authenticated user lacks permission to access the resource.
        /// </summary>
        public const string Forbidden = "forbidden";

        /// <summary>
        /// Error code for 409 Conflict responses.
        /// Indicates the request conflicts with the current state of the resource.
        /// </summary>
        public const string Conflict = "conflict";

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
        /// Error code for 401 Unauthorized responses when JWT token is missing.
        /// </summary>
        public const string JwtAuthenticationRequired = "jwt_authentication_required";

        /// <summary>
        /// Error code for 401 Unauthorized responses when JWT credentials are invalid.
        /// </summary>
        public const string JwtInvalidCredentials = "jwt_invalid_credentials";

        /// <summary>
        /// Error code when JWT token has expired.
        /// Frontend should refresh the token or redirect to login.
        /// </summary>
        public const string JwtTokenExpired = "jwt_token_expired";

        /// <summary>
        /// Error code when JWT token is invalid or malformed.
        /// </summary>
        public const string JwtInvalidToken = "jwt_invalid_token";

        /// <summary>
        /// Error code when JWT token signature validation fails.
        /// Indicates potential token tampering.
        /// </summary>
        public const string JwtInvalidSignature = "jwt_invalid_signature";

        /// <summary>
        /// Error code when JWT token issuer does not match expected value.
        /// </summary>
        public const string JwtInvalidIssuer = "jwt_invalid_issuer";

        /// <summary>
        /// Error code when JWT token audience does not match expected value.
        /// </summary>
        public const string JwtInvalidAudience = "jwt_invalid_audience";
    }
