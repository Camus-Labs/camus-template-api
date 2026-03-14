using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using emc.camus.api.Configurations;
using emc.camus.api.Metrics;
using emc.camus.application.Common;
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
        private const int RegexTimeoutMilliseconds = 100;

        /// <summary>
        /// Platform-defined error code mapping rules that cannot be changed via configuration.
        /// These rules map common exception types to their corresponding error codes.
        /// Additional rules can be added via configuration (ErrorHandlingSettings.AdditionalRules).
        /// </summary>
        private static readonly IReadOnlyList<ErrorCodeMappingRule> PlatformRules = new List<ErrorCodeMappingRule>
        {
            new() { Type = nameof(RateLimitExceededException), ErrorCode = ErrorCodes.RateLimitExceeded },
            new() { Type = nameof(KeyNotFoundException), ErrorCode = ErrorCodes.NotFound },
            // JWT-specific error patterns (most specific first)
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "jwt.*expired|token.*expired", ErrorCode = ErrorCodes.JwtTokenExpired },
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "invalid.*signature", ErrorCode = ErrorCodes.JwtInvalidSignature },
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "invalid.*issuer", ErrorCode = ErrorCodes.JwtInvalidIssuer },
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "invalid.*audience", ErrorCode = ErrorCodes.JwtInvalidAudience },
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "revoked", ErrorCode = ErrorCodes.JwtTokenRevoked },
            // Authentication required patterns (checked before generic invalid credentials)
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "authentication.*required.*jwt", ErrorCode = ErrorCodes.JwtAuthenticationRequired },
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "authentication.*required.*api.?key", ErrorCode = ErrorCodes.ApiKeyAuthenticationRequired },
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "authentication.*required", ErrorCode = ErrorCodes.AuthenticationRequired },
            // Specific invalid credentials patterns (checked before generic)
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "invalid.*jwt|jwt.*invalid", ErrorCode = ErrorCodes.JwtInvalidCredentials },
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "invalid.*api.?key", ErrorCode = ErrorCodes.ApiKeyInvalidCredentials },
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "username.*password|password.*mismatch", ErrorCode = ErrorCodes.AuthInvalidCredentials },
            new() { Type = nameof(UnauthorizedAccessException), Pattern = "invalid|credentials|incorrect", ErrorCode = ErrorCodes.InvalidCredentials },
            new() { Type = nameof(UnauthorizedAccessException), ErrorCode = ErrorCodes.Unauthorized },
            new() { Type = nameof(ArgumentException), ErrorCode = ErrorCodes.BadRequest },
            new() { Type = nameof(InvalidOperationException), Pattern = "not.?found", ErrorCode = ErrorCodes.NotFound },
            new() { Type = nameof(InvalidOperationException), Pattern = "permission", ErrorCode = ErrorCodes.Forbidden },
            new() { Pattern = "secret|configuration", ErrorCode = ErrorCodes.InternalServerError }
        }.AsReadOnly();

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;
        private readonly IReadOnlyList<ErrorCodeMappingRule> _allRules;
        private readonly ErrorMetrics _errorMetrics;

        /// <summary>
        /// ExceptionHandlingMiddleware constructor initializes the middleware with a request delegate, logger, host environment, and error handling settings.
        /// This constructor is used to set up the middleware in the ASP.NET Core pipeline, allowing it to intercept requests and handle exceptions.
        /// The host environment is used to determine detail level for error responses (verbose in Development, minimal in Production).
        /// </summary>
        /// <param name="next">The next middleware in the pipeline to invoke after processing</param>
        /// <param name="logger">Logger instance for recording exception information</param>
        /// <param name="environment">Host environment to determine error detail level</param>
        /// <param name="settings">Error handling settings containing error code mapping rules</param>
        /// <param name="errorMetrics">Metrics service for tracking error responses</param>
        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment,
            IOptions<ErrorHandlingSettings> settings,
            ErrorMetrics errorMetrics)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(environment);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(errorMetrics);

            _next = next;
            _logger = logger;
            _environment = environment;
            _errorMetrics = errorMetrics;

            // Combine rules once at startup: AdditionalRules first (allow config overrides), then PlatformRules
            _allRules = settings.Value.AdditionalRules.Concat(PlatformRules).ToList().AsReadOnly();
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
            var problemDetails = CreateProblemDetails(exception);

            problemDetails.Instance = context.Request.Path;

            AddErrorCode(context, problemDetails, exception);

            if (_environment.IsDevelopment())
            {
                AddDevelopmentInformation(problemDetails, exception);
            }

            _logger.LogError(exception, "Exception detected: {ErrorMessage}", exception.Message);

            context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
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
        private ProblemDetails CreateProblemDetails(Exception exception)
        {
            var problemDetails = exception switch
            {
                ArgumentException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.BadRequest),
                    Detail = exception.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                },
                KeyNotFoundException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.NotFound),
                    Detail = exception.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
                },
                UnauthorizedAccessException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.Unauthorized),
                    Detail = "You are not authorized to access this resource.",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
                },
                RateLimitExceededException rateLimitEx => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.TooManyRequests,
                    Title = ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.TooManyRequests),
                    Detail = "Rate limit exceeded. Please try again later.",
                    Type = "https://tools.ietf.org/html/rfc6585#section-4",
                    Extensions = { ["retryAfter"] = rateLimitEx.RetryAfterSeconds }
                },
                InvalidOperationException when exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.NotFound),
                    Detail = exception.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
                },
                InvalidOperationException when exception.Message.Contains("permission") => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Title = ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.Forbidden),
                    Detail = "You do not have permission to access this resource.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
                },
                InvalidOperationException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Conflict,
                    Title = ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.Conflict),
                    Detail = exception.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
                },
                _ => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.InternalServerError),
                    Detail = "An unexpected error occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                }
            };

            return problemDetails;
        }

        /// <summary>
        /// Adds machine-readable error code to the problem details.
        /// Maps exception to error code using configured rules.
        /// </summary>
        private void AddErrorCode(HttpContext context, ProblemDetails problemDetails, Exception exception)
        {
            var errorCode = ResolveErrorCode(exception);
            problemDetails.Extensions["error"] = errorCode;

            // Normalize path to route template to ensure low-cardinality metric labels
            var endpoint = context.GetEndpoint();
            var routePattern = (endpoint as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern?.RawText
                ?? problemDetails.Instance
                ?? "unknown";

            // Record error metrics (fire-and-forget telemetry)
            _errorMetrics.RecordError(errorCode, problemDetails.Status ?? (int)HttpStatusCode.InternalServerError, routePattern);
        }

        /// <summary>
        /// Resolves an exception to a machine-readable error code using configured rules.
        /// Evaluates AdditionalRules (from configuration) before PlatformRules (built-in).
        /// </summary>
        private string ResolveErrorCode(Exception exception)
        {
            // First check if error code was explicitly set in exception.Data (override mechanism)
            if (exception.Data.Contains(ErrorCodes.ErrorCodeKey) &&
                exception.Data[ErrorCodes.ErrorCodeKey] is string explicitCode &&
                !string.IsNullOrWhiteSpace(explicitCode))
            {
                _logger.LogWarning(
                    "Explicit error code '{ExplicitCode}' found in exception.Data[{ErrorCodeKey}]. " +
                    "This pattern is discouraged - error codes should be automatically detected via configuration. " +
                    "Exception type: {ExceptionType}",
                    explicitCode, ErrorCodes.ErrorCodeKey, exception.GetType().Name);
                return explicitCode;
            }

            // Evaluate rules in order - first match wins
            var exceptionTypeName = exception.GetType().Name;

            foreach (var rule in _allRules)
            {
                // Check type match (if specified)
                bool typeMatches = string.IsNullOrWhiteSpace(rule.Type) ||
                                   exceptionTypeName.Equals(rule.Type, StringComparison.OrdinalIgnoreCase);

                if (!typeMatches)
                    continue;

                // Check pattern match (if specified)
                if (!string.IsNullOrWhiteSpace(rule.Pattern))
                {
                    try
                    {
                        if (Regex.IsMatch(exception.Message, rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(RegexTimeoutMilliseconds)))
                        {
                            return rule.ErrorCode;
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        _logger.LogWarning(
                            "Regex pattern '{Pattern}' timed out matching exception message. Skipping rule.",
                            rule.Pattern);
                        continue;
                    }
                }
                else
                {
                    // Type-only match (no pattern specified)
                    return rule.ErrorCode;
                }
            }

            // No rules matched - return unknown error constant
            return ErrorCodes.DefaultErrorCode;
        }

        /// <summary>
        /// Adds development debugging information to the problem details.
        /// </summary>
        private void AddDevelopmentInformation(ProblemDetails problemDetails, Exception exception)
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["exceptionMessage"] = exception.Message;

            // Add inner exception chain if present
            if (exception.InnerException != null)
            {
                var innerExceptions = new List<object>();
                var currentException = exception.InnerException;

                while (currentException != null)
                {
                    innerExceptions.Add(new
                    {
                        type = currentException.GetType().Name,
                        message = currentException.Message
                    });
                    currentException = currentException.InnerException;
                }

                problemDetails.Extensions["innerExceptions"] = innerExceptions;
            }

            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }
        }

    }
}
