using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Swashbuckle.AspNetCore.Annotations;
using emc.camus.application.Observability;
using emc.camus.application.Auth;
using Microsoft.AspNetCore.Authorization;
using emc.camus.application.RateLimiting;
using emc.camus.api.Models.Requests.V2;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;
using emc.camus.api.Models.Dtos.V2;
using emc.camus.api.Mapping;
using emc.camus.api.Mapping.V2;
using Microsoft.AspNetCore.Http.Timeouts;
using emc.camus.api.Configurations;

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
    /// Configure rate limit policies in appsettings.json under InMemoryRateLimitingSettings.Policies.
    /// </remarks>
    [Authorize]
    [RateLimit(RateLimitPolicies.Strict)]
    [RequestTimeout(RequestTimeoutPolicies.Default)]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    public class AuthController : ApiControllerBase
    {
        private readonly IActivitySourceWrapper _activitySource;
        private readonly IAuthService _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="activitySource">Activity source for OpenTelemetry tracing.</param>
        /// <param name="authService">Authentication service for credential validation and token generation.</param>
        public AuthController(
            IActivitySourceWrapper activitySource,
            IAuthService authService)
        {
            ArgumentNullException.ThrowIfNull(activitySource);
            ArgumentNullException.ThrowIfNull(authService);

            _activitySource = activitySource;
            _authService = authService;
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token for valid credentials. Available for API version >=2.0.
        /// Requires API Key authentication to access this endpoint.
        /// </summary>
        /// <param name="request">The authentication request containing Username and Password.</param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>Authentication response with JWT token if credentials are valid; otherwise, an error response.</returns>
        [HttpPost("authenticate")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.ApiKey)]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Authenticates a user and generates a JWT token for valid credentials in API version >=2.0. Requires API Key authentication."
        )]
        [ProducesResponseType(typeof(ApiResponse<AuthenticateUserResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AuthenticateUser([FromBody] AuthenticateUserRequest request, CancellationToken ct)
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
                var result = await _authService.AuthenticateAsync(command, ct);

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "expiresOn", result.ExpiresOn.ToString("o") }
                });

                // Use base controller helper for standardized response
                return Success(result.ToResponse(), "User authenticated successfully");
            });
        }

        /// <summary>
        /// Generates a custom token with specified permissions and expiration for an authenticated user.
        /// Requires JWT authentication and token.create permission.
        /// Available for API version >=2.0.
        /// </summary>
        /// <param name="request">The token generation request containing suffix, expiration, and permissions.</param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>Token generation response with token details and permissions.</returns>
        [HttpPost("generate-token")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.JwtBearer, Policy = Permissions.TokenCreate)]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Generates a custom token with specified permissions and expiration. Requires token.create permission."
        )]
        [ProducesResponseType(typeof(ApiResponse<GenerateTokenResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenRequest request, CancellationToken ct)
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GenerateToken", OperationType.Auth, async activity =>
            {
                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "username_suffix", request.UsernameSuffix },
                    { "requested_permissions", string.Join(",", request.Permissions) },
                    { "expiresOn", request.ExpiresOn.ToString("o") }
                });

                // Map API DTO to Application Command
                var command = request.ToCommand();

                // Call authentication service to generate token
                var result = await _authService.GenerateTokenAsync(command, ct);

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "token_username", result.TokenUsername }
                });

                // Use base controller helper for standardized response
                return Created(result.ToResponse(), "Token generated successfully");
            });
        }

        /// <summary>
        /// Retrieves a paginated list of generated tokens created by the currently authenticated user.
        /// Supports filtering by revocation and expiration status.
        /// Requires JWT authentication and token.create permission.
        /// Available for API version >=2.0.
        /// </summary>
        /// <param name="query">Query parameters including pagination and filters.</param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>A paginated list of generated token summaries.</returns>
        [HttpGet("tokens")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.JwtBearer, Policy = Permissions.TokenCreate)]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Retrieves a paginated list of generated tokens for the current user. Supports filtering by revocation/expiration status. Requires token.create permission."
        )]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGeneratedTokens([FromQuery] GetGeneratedTokensQuery query, CancellationToken ct)
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetGeneratedTokens", OperationType.Auth, async activity =>
            {
                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "page", query.Page },
                    { "page_size", query.PageSize },
                    { "exclude_revoked", query.ExcludeRevoked },
                    { "exclude_expired", query.ExcludeExpired }
                });

                var pagination = query.ToPaginationParams();
                var filter = query.ToFilter();
                var pagedResult = await _authService.GetGeneratedTokensAsync(pagination, filter, ct);

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "token_count", pagedResult.TotalCount },
                    { "page", pagedResult.Page },
                    { "page_size", pagedResult.PageSize }
                });

                var data = pagedResult.ToPagedResponse(r => r.ToDto());

                return Success(data, $"Retrieved {pagedResult.Items.Count} of {pagedResult.TotalCount} generated tokens");
            });
        }

        /// <summary>
        /// Revokes a generated token by its JTI (JWT ID).
        /// Only the creator of the token can revoke it.
        /// Requires JWT authentication and token.create permission.
        /// Available for API version >=2.0.
        /// </summary>
        /// <param name="jti">The JWT ID of the token to revoke.</param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>The revoked token summary confirming the revocation.</returns>
        [HttpPost("tokens/{jti:guid}/revoke")]
        [Authorize(AuthenticationSchemes = AuthenticationSchemes.JwtBearer, Policy = Permissions.TokenCreate)]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Revokes a generated token by JTI. Only the token creator can revoke it. Requires token.create permission."
        )]
        [ProducesResponseType(typeof(ApiResponse<GeneratedTokenSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RevokeToken([FromRoute] Guid jti, CancellationToken ct)
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("RevokeToken", OperationType.Auth, async activity =>
            {
                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "jti", jti }
                });

                // Map route inputs to Application Command
                var command = Mapping.V2.AuthMappingExtensions.ToRevokeTokenCommand(jti);
                var result = await _authService.RevokeTokenAsync(command, ct);

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "token_username", result.TokenUsername },
                    { "is_revoked", result.IsRevoked },
                    { "revoked_at", result.RevokedAt?.ToString("o") }
                });

                return Success(result.ToDto(), "Token revoked successfully");
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
            Description = "Handles unexpected errors in API version 2.0"
        )]
        public async Task<IActionResult> GetUnexpectedError(CancellationToken ct)
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetUnexpectedError", OperationType.Test, activity =>
            {
                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "demoKey", "demoValue" }
                });

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "error_type", "demo_exception" }
                });

                throw new InvalidOperationException("This is a demo exception for error handling.", new InvalidProgramException("Inner exception for demo purposes."));
            });
        }
    }
}
