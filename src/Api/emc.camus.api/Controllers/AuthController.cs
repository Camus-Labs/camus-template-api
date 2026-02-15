using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Swashbuckle.AspNetCore.Annotations;
using emc.camus.domain.Generic;
using emc.camus.application.Observability;
using emc.camus.application.Secrets;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using Microsoft.AspNetCore.Authorization;
using emc.camus.domain.Auth;
using System.Security.Claims;
using emc.camus.application.RateLimiting;
using emc.camus.api.Models.Requests;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Extensions;

namespace emc.camus.api.Controllers
{
    /// <summary>
    /// Handles authentication endpoints, including JWT token generation, API key authentication, and API info retrieval per version.
    /// </summary>
    /// <remarks>
    /// Provides endpoints for public API info, API key-protected info, JWT-protected info, and token generation. 
    /// Integrates with OpenTelemetry for activity tracing and logs API version for observability.
    /// 
    /// Rate Limiting: Uses strict policy (lower limits) to protect authentication endpoints from brute force attacks.
    /// Configure rate limit policies in appsettings.json under RateLimitSettings.Policies.
    /// </remarks>
    [Authorize]
    [RateLimit(RateLimitPolicies.Strict)]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IActivitySourceWrapper _activitySource;
        private readonly AuthService _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration provider.</param>
        /// <param name="logger">Logger for AuthController.</param>
        /// <param name="activitySource">Activity source for OpenTelemetry tracing.</param>
        /// <param name="authService">Authentication service for credential validation and token generation.</param>
        public AuthController(
            IConfiguration configuration, 
            ILogger<AuthController> logger, 
            IActivitySourceWrapper activitySource,
            AuthService authService)
        {
            _logger = logger;
            _configuration = configuration;
            _activitySource = activitySource;
            _authService = authService;
        }

        /// <summary>
        /// Returns public API information for version 1.0. No authentication required.
        /// </summary>
        /// <returns>API info for v1.0.</returns>
        [HttpGet("info")]
        [AllowAnonymous]
        [RateLimit(RateLimitPolicies.Relaxed)]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Allows public information request about the API for version 1.0, including features and timestamp."
        )]
        [ProducesResponseType(typeof(ApiResponse<ApiInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInfoV1()
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetInfoV1", OperationType.Read, activity =>
            {
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "unknown";

                var features = new List<string>
                {
                    "Authentication",
                    "Authorization",
                    "Observability"
                };

                var apiInfo = new ApiInfo(apiVersion, features: features);

                var response = new ApiResponse<ApiInfo>
                {
                    Message = "API information retrieved successfully",
                    Data = apiInfo
                };

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "features", string.Join(",", apiInfo.Features) },
                    { "status", apiInfo.Status }
                });
                return Task.FromResult<IActionResult>(Ok(response));
            });
        }

        /// <summary>
        /// Returns API information for version 2.0. Requires API Key authentication.
        /// </summary>
        /// <returns>API info for v2.0 (API Key required).</returns>
        [HttpGet("info-apikey")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.ApiKey)]
        [RateLimit(RateLimitPolicies.Default)]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Returns API info for v2.0, requires API Key authentication."
        )]
        [ProducesResponseType(typeof(ApiResponse<ApiInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInfoV2ApiKey()
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetInfoV2ApiKey", OperationType.Read, activity =>
            {
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "unknown";

                var features = new List<string>
                {
                    "Authentication",
                    "Authorization",
                    "Versioning",
                    "Observability",
                    "Rate Limiting",
                    "Swagger/OpenAPI",
                    "Secret Management",
                    "Error Handling",
                    "CORS"
                };

                var apiInfo = new ApiInfo(apiVersion, "API Key Authentication", features: features);

                var response = new ApiResponse<ApiInfo>
                {
                    Message = "API information retrieved successfully",
                    Data = apiInfo
                };

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "features", string.Join(",", apiInfo.Features) },
                    { "status", apiInfo.Status }
                });
                return Task.FromResult<IActionResult>(Ok(response));
            });
        }

        /// <summary>
        /// Returns API information for version 2.0. Requires JWT authentication.
        /// </summary>
        /// <returns>API info for v2.0 (JWT required).</returns>
        [HttpGet("info-jwt")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.JwtBearer)]
        [RateLimit(RateLimitPolicies.Default)]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Returns API info for v2.0, requires JWT authentication."
        )]
        [ProducesResponseType(typeof(ApiResponse<ApiInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInfoV2Jwt()
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetInfoV2Jwt", OperationType.Read, activity =>
            {
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "unknown";

                var features = new List<string>
                {
                    "Authentication",
                    "Authorization",
                    "Versioning",
                    "Observability",
                    "Rate Limiting",
                    "Swagger/OpenAPI",
                    "Secret Management",
                    "Error Handling",
                    "CORS"
                };

                var apiInfo = new ApiInfo(apiVersion, "JWT Key Authentication", features: features);

                var response = new ApiResponse<ApiInfo>
                {
                    Message = "API information retrieved successfully",
                    Data = apiInfo
                };

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "features", string.Join(",", apiInfo.Features) },
                    { "status", apiInfo.Status }
                });
                return Task.FromResult<IActionResult>(Ok(response));
            });
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token for valid credentials. Available for API version >=2.0.
        /// Requires API Key authentication to access this endpoint.
        /// </summary>
        /// <param name="request">The authentication request containing Username and Password.</param>
        /// <returns>Authentication response with JWT token if credentials are valid; otherwise, an error response.</returns>
        [HttpPost("authenticate")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.ApiKey)]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Authenticates a user and generates a JWT token for valid credentials in API version >=2.0. Requires API Key authentication."
        )]
        [ProducesResponseType(typeof(ApiResponse<AuthenticateUserResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AuthenticateUser([FromBody] AuthenticateUserRequest request)
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("AuthenticateUser", OperationType.Auth, async activity =>
            {
                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "username", request.Username }
                });

                // Map API DTO to Application Command
                var command = request.ToCommand();

                // Call authentication service (business logic encapsulated)
                var result = await _authService.AuthenticateAsync(command);

                // Map Application Result to API Response
                var response = new ApiResponse<AuthenticateUserResponse>
                {
                    Message = "User authenticated successfully",
                    Data = result.ToResponse()
                };

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "expiresOn", result.ExpiresOn.ToString("o") }
                });

                return Ok(response);
            });
        }

        /// <summary>
        /// Endpoint to trigger an unexpected error for demonstration and error handling testing.
        /// </summary>
        /// <returns>Error message.</returns>
        [HttpPost("unexpected-error")]
        [AllowAnonymous]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Handles unexpected errors in API version 1.0"
        )]
        public async Task<IActionResult> GetUnexpectedError()
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetUnexpectedError", OperationType.Test, activity =>
            {
                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "demoKey", "demoValue" }
                });

                _logger.LogWarning("This is a demo warning for error handling.");
                throw new Exception("This is a demo exception for error handling.", new Exception("Inner exception for demo purposes."));
            });
        }
    }
}
