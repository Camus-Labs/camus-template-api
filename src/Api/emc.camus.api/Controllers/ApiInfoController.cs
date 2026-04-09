using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Swashbuckle.AspNetCore.Annotations;
using emc.camus.application.Observability;
using emc.camus.application.ApiInfo;
using Microsoft.AspNetCore.Authorization;
using emc.camus.application.RateLimiting;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V1;
using emc.camus.api.Mapping.V1;
using emc.camus.application.Auth;
using Microsoft.AspNetCore.Http.Timeouts;
using emc.camus.api.Configurations;

namespace emc.camus.api.Controllers
{
    /// <summary>
    /// Handles API information endpoints that provide metadata about available API versions and features.
    /// </summary>
    /// <remarks>
    /// Provides endpoints for retrieving API information with different authentication requirements:
    /// - Public endpoint (no auth)
    /// - API Key protected
    /// - JWT protected
    ///
    /// Integrates with OpenTelemetry for activity tracing and logs API version for observability.
    ///
    /// Rate Limiting: Uses relaxed policy for public endpoints, default for authenticated endpoints.
    /// Configure rate limit policies in appsettings.json under RateLimitSettings.Policies.
    /// </remarks>
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [RequestTimeout(RequestTimeoutPolicies.Tight)]
    [ApiController]
    public class ApiInfoController : ApiControllerBase
    {
        private readonly IActivitySourceWrapper _activitySource;
        private readonly IApiInfoService _apiInfoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiInfoController"/> class.
        /// </summary>
        /// <param name="activitySource">Activity source for OpenTelemetry tracing.</param>
        /// <param name="apiInfoService">Application service for retrieving API information.</param>
        public ApiInfoController(
            IActivitySourceWrapper activitySource,
            IApiInfoService apiInfoService)
        {
            ArgumentNullException.ThrowIfNull(activitySource);
            ArgumentNullException.ThrowIfNull(apiInfoService);

            _activitySource = activitySource;
            _apiInfoService = apiInfoService;
        }

        /// <summary>
        /// Returns public API information for version 1.0 and 2.0. No authentication required.
        /// </summary>
        /// <returns>API info for the requested version.</returns>
        [HttpGet("info")]
        [AllowAnonymous]
        [RateLimit(RateLimitPolicies.Relaxed)]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Allows public information request about the API, including features and status. Available for all versions."
        )]
        [ProducesResponseType(typeof(ApiResponse<ApiInfoResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInfo(CancellationToken ct)
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetInfo", OperationType.Read, async activity =>
            {
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "unknown";

                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "api_version", apiVersion }
                });

                // Call application service to retrieve API info
                var filter = ApiInfoMappingExtensions.ToFilter(apiVersion);
                var result = await _apiInfoService.GetByVersionAsync(filter, ct);

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "features", string.Join(",", result.Features) },
                    { "status", result.Status }
                });

                // Use base controller helper for standardized response
                return Success(result.ToResponse(), "API information retrieved successfully");
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
        [ProducesResponseType(typeof(ApiResponse<ApiInfoResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInfoApiKey(CancellationToken ct)
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetInfoApiKey", OperationType.Read, async activity =>
            {
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "unknown";

                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "api_version", apiVersion }
                });

                // Call application service to retrieve API info
                var filter = ApiInfoMappingExtensions.ToFilter(apiVersion);
                var result = await _apiInfoService.GetByVersionAsync(filter, ct);

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "features", string.Join(",", result.Features) },
                    { "status", result.Status }
                });

                // Use base controller helper for standardized response
                return Success(result.ToResponse(), "API information retrieved successfully");
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
        [ProducesResponseType(typeof(ApiResponse<ApiInfoResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInfoJwt(CancellationToken ct)
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetInfoJwt", OperationType.Read, async activity =>
            {
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "unknown";

                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "api_version", apiVersion }
                });

                // Call application service to retrieve API info
                var filter = ApiInfoMappingExtensions.ToFilter(apiVersion);
                var result = await _apiInfoService.GetByVersionAsync(filter, ct);

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "features", string.Join(",", result.Features) },
                    { "status", result.Status }
                });

                // Use base controller helper for standardized response
                return Success(result.ToResponse(), "API information retrieved successfully");
            });
        }
    }
}
