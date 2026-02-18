using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Swashbuckle.AspNetCore.Annotations;
using emc.camus.application.Observability;
using emc.camus.application.Auth;
using Microsoft.AspNetCore.Authorization;
using emc.camus.application.RateLimiting;
using emc.camus.api.Models.Requests;
using emc.camus.api.Models.Responses;
using emc.camus.api.Mapping;

namespace emc.camus.api.Controllers
{
    /// <summary>
    /// Handles authentication endpoints, including JWT token generation and user authentication.
    /// </summary>
    /// <remarks>
    /// Provides endpoints for user authentication and token generation.
    /// Integrates with OpenTelemetry for activity tracing.
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
    public class AuthController : ApiControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IActivitySourceWrapper _activitySource;
        private readonly AuthService _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="logger">Logger for AuthController.</param>
        /// <param name="activitySource">Activity source for OpenTelemetry tracing.</param>
        /// <param name="authService">Authentication service for credential validation and token generation.</param>
        public AuthController(
            ILogger<AuthController> logger, 
            IActivitySourceWrapper activitySource,
            AuthService authService)
        {
            _logger = logger;
            _activitySource = activitySource;
            _authService = authService;
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

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "expiresOn", result.ExpiresOn.ToString("o") }
                });

                // Use base controller helper for standardized response
                return Success(result.ToResponse(), "User authenticated successfully");
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
