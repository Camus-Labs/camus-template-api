using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Swashbuckle.AspNetCore.Annotations;
using emc.camus.domain.Generic;
using emc.camus.domain.Logging;
using emc.camus.application.Secrets;
using Microsoft.AspNetCore.Authorization;
using emc.camus.domain.Auth;
using emc.camus.main.api.Configurations;
using emc.camus.main.api.Handlers;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace emc.camus.main.api.Controllers
{
    /// <summary>
    /// Handles authentication endpoints, including JWT token generation, API key authentication, and API info retrieval per version.
    /// </summary>
    /// <remarks>
    /// Provides endpoints for public API info, API key-protected info, JWT-protected info, and token generation. Integrates with OpenTelemetry for activity tracing and logs API version for observability.
    /// </remarks>
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ISecretProvider _secretProvider;
        private readonly ILogger<AuthController> _logger;
        private readonly IActivitySourceWrapper _activitySource;
        private readonly JwtSettings _jwtSettings;
        private readonly SigningCredentials _signingCredentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration provider.</param>
        /// <param name="logger">Logger for AuthController.</param>
        /// <param name="activitySource">Activity source for OpenTelemetry tracing.</param>
        /// <param name="secretProvider">Provider for retrieving secrets and credentials.</param>
        /// <param name="jwtSettings">JWT settings from configuration.</param>
        /// <param name="signingCredentials">Signing credentials for JWT tokens.</param>
        public AuthController(
            IConfiguration configuration, 
            ILogger<AuthController> logger, 
            IActivitySourceWrapper activitySource,
            ISecretProvider secretProvider,
            IOptions<JwtSettings> jwtSettings,
            SigningCredentials signingCredentials)
        {
            _logger = logger;
            _configuration = configuration;
            _activitySource = activitySource;
            _secretProvider = secretProvider;
            _jwtSettings = jwtSettings.Value;
            _signingCredentials = signingCredentials;
        }

        /// <summary>
        /// Returns public API information for version 1.0. No authentication required.
        /// </summary>
        /// <returns>API info for v1.0.</returns>
        [HttpGet("info")]
        [AllowAnonymous]
        [MapToApiVersion("1.0")]
        [SwaggerOperation(
            Description = "Allows public information request about the API for version 1.0, including features and timestamp."
        )]
        [ProducesResponseType(typeof(ApiInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInfoV1()
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetInfoV1", OperationType.Read, activity =>
            {
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "unknown";
                _logger.LogInformation("API info v{Version} requested.", apiVersion);

                var response = new ApiInfo
                {
                    Name = "My Basic API",
                    Version = apiVersion,
                    Status = $"Running with API Versioning v{apiVersion}",
                    Features = new List<string> { "Logging", "Versioning", "Authentication", "Authorization", "Observability" },
                    Timestamp = DateTime.UtcNow
                };

                var ts = response.Timestamp is DateTime dt ? dt.ToString("o") : response.Timestamp?.ToString() ?? string.Empty;
                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "features", string.Join(",", response.Features) },
                    { "timestamp", ts },
                    { "status", response.Status }
                });
                _logger.LogInformation("API info v{Version} retrieved.", apiVersion);
                return Task.FromResult<IActionResult>(Ok(response));
            });
        }

        /// <summary>
        /// Returns API information for version 2.0. Requires API Key authentication.
        /// </summary>
        /// <returns>API info for v2.0 (API Key required).</returns>
        [HttpGet("info-apikey")]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Returns API info for v2.0, requires API Key authentication."
        )]
        [ProducesResponseType(typeof(ApiInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetInfoV2ApiKey()
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetInfoV2ApiKey", OperationType.Read, activity =>
            {
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "unknown";
                _logger.LogInformation("API info v{Version} (API Key) requested.", apiVersion);

                var response = new ApiInfo
                {
                    Name = "My Basic API",
                    Version = apiVersion,
                    Status = $"Running with API Versioning v{apiVersion} (API Key)",
                    Features = new List<string> { "Logging", "Versioning", "Authentication", "Authorization", "Observability" },
                    Timestamp = DateTime.UtcNow
                };

                var ts = response.Timestamp is DateTime dt ? dt.ToString("o") : response.Timestamp?.ToString() ?? string.Empty;
                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "features", string.Join(",", response.Features) },
                    { "timestamp", ts },
                    { "status", response.Status }
                });
                _logger.LogInformation("API info v{Version} (API Key) retrieved.", apiVersion);
                return Task.FromResult<IActionResult>(Ok(response));
            });
        }

        /// <summary>
        /// Returns API information for version 2.0. Requires JWT authentication.
        /// </summary>
        /// <returns>API info for v2.0 (JWT required).</returns>
        [HttpGet("info-jwt")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Returns API info for v2.0, requires JWT authentication."
        )]
        [ProducesResponseType(typeof(ApiInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetInfoV2Jwt()
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GetInfoV1Jwt", OperationType.Read, activity =>
            {
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "unknown";
                _logger.LogInformation("API info v{Version} (JWT) requested.", apiVersion);

                var response = new ApiInfo
                {
                    Name = "My Basic API",
                    Version = apiVersion,
                    Status = $"Running with API Versioning v{apiVersion} (JWT)",
                    Features = new List<string> { "Logging", "Versioning", "Authentication", "Authorization", "Observability" },
                    Timestamp = DateTime.UtcNow
                };

                var ts = response.Timestamp is DateTime dt ? dt.ToString("o") : response.Timestamp?.ToString() ?? string.Empty;
                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "features", string.Join(",", response.Features) },
                    { "timestamp", ts },
                    { "status", response.Status }
                });
                _logger.LogInformation("API info v{Version} (JWT) retrieved.", apiVersion);
                return Task.FromResult<IActionResult>(Ok(response));
            });
        }

        /// <summary>
        /// Generates a JWT token for valid credentials. Available for API version >=2.0.
        /// </summary>
        /// <param name="request">The JWT token request containing AccessKey and AccessSecret.</param>
        /// <returns>JWT token response if credentials are valid; otherwise, an error response.</returns>
        [HttpPost("token")]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Generates a JWT token for valid credentials in API version >=2.0"
        )]
        [ProducesResponseType(typeof(AuthToken), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateToken([FromBody] Credentials request)
        {
            return await _activitySource.StartActivityAndRunAsync<IActionResult>("GenerateToken", OperationType.Auth, activity =>
            {
                _activitySource.SetRequestTags(activity, new Dictionary<string, object?>
                {
                    { "accessKey", request.AccessKey }
                });

                _logger.LogInformation("Token request received for AccessKey: {AccessKey}.", request.AccessKey);

                // Validate credentials (in production, validate against database)
                var accessKeyFromVault = _secretProvider.GetSecret("AccessKey");
                var accessSecretFromVault = _secretProvider.GetSecret("AccessSecret");

                if (request.AccessKey != accessKeyFromVault || request.AccessSecret != accessSecretFromVault)
                {
                    _logger.LogWarning("Invalid credentials provided for AccessKey: {AccessKey}.", request.AccessKey);
                    throw new UnauthorizedAccessException("Invalid credentials.");
                }

                _logger.LogInformation("Valid credentials provided. Generating JWT token.");

                // Generate JWT token
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, request.AccessKey ?? "unknown"),
                    new Claim(JwtRegisteredClaimNames.UniqueName, request.AccessKey ?? "unknown"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim(ClaimTypes.Role, "ApiClient")
                };

                var expiresOn = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

                var jwtToken = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires: expiresOn,
                    signingCredentials: _signingCredentials
                );

                var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

                var response = new AuthToken
                {
                    Token = token,
                    ExpiresOn = expiresOn
                };

                _activitySource.SetResponseTags(activity, new Dictionary<string, object?>
                {
                    { "expiresOn", response.ExpiresOn.ToString("o") }
                });

                _logger.LogInformation("JWT token generated for user: {User}, expires: {Expiration}", 
                    request.AccessKey, expiresOn);

                return Task.FromResult<IActionResult>(Ok(response));
            });
        }

        /// <summary>
        /// Endpoint to trigger an unexpected error for demonstration and error handling testing.
        /// </summary>
        /// <returns>Error message.</returns>
        [HttpPost("unexpected-error")]
        [MapToApiVersion("1.0")]
        [SwaggerOperation(
            Description = "Handles unexpected errors in API version 1.0"
        )]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
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