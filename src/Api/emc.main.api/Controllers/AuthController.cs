using emc.camus.main.api.Models;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Swashbuckle.AspNetCore.Annotations;
using emc.camus.domain.Generic;

namespace emc.camus.main.api.Controllers
{

    /// <summary>
    /// AuthController
    /// </summary>
    /// <remarks>
    /// Purpose: Handles authentication endpoints, including token generation and API info per version.
    /// Usage: Call endpoints to retrieve API info or request JWT tokens.
    /// Output: JSON responses with API info or JWT tokens.
    /// Dependencies: IConfiguration, ILogger&lt;AuthController&gt;
    /// </remarks>
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes the AuthController with configuration and logger.
        /// </summary>
        /// <param name="configuration">Application configuration provider.</param>
        /// <param name="logger">Logger for AuthController.</param>
        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Returns API information for version 1.0.
        /// </summary>
        /// <returns>API info for v1.0</returns>
        [HttpGet("info")]
        [MapToApiVersion("1.0")]
        [SwaggerOperation(
            Description = "Returns basic information about the API for version 1.0."
        )]
        [ProducesResponseType(typeof(ApiInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult GetInfoV10()
        {
            _logger.LogInformation("API info v1.0 requested.");

            var info = new ApiInfo
            {
                Name = "My Basic API",
                Version = "1.0.0",
                Status = "Running with API Versioning v1"
            };

            return Ok(info);
        }

        /// <summary>
        /// Returns API information for version 2.0.
        /// </summary>
        /// <returns>API info for v2.0</returns>
        [HttpGet("info")]
        [MapToApiVersion("2.0")]
        [SwaggerOperation(
            Description = "Returns extended information about the API for version 2.0, including features and timestamp."
        )]
        [ProducesResponseType(typeof(ApiInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult GetInfoV20()
        {
            _logger.LogInformation("API info v2.0 requested.");

            var info = new ApiInfo
            {
                Name = "My Basic API",
                Version = "2.0.0",
                Status = "Running with API Versioning v2",
                Features = new List<string> { "Logging", "Versioning" },
                Timestamp = DateTime.UtcNow
            };

            return Ok(info);
        }

        /// <summary>
        /// Returns API information for version 3.0.
        /// </summary>
        /// <returns>API info for v3.0</returns>
        [HttpGet("info")]
        [MapToApiVersion("3.0")]
        [SwaggerOperation(
            Description = "Returns extended information about the API for version 3.0, including features and timestamp."
        )]
        [ProducesResponseType(typeof(ApiInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult GetInfoV3()
        {
            _logger.LogInformation("API info v3.0 requested.");

            var info = new ApiInfo
            {
                Name = "My Basic API",
                Version = "3.0.0",
                Status = "Running with API Versioning v3",
                Features = new List<string> { "Logging", "Versioning" },
                Timestamp = DateTime.UtcNow
            };

            return Ok(info);
        }

        /// <summary>
        /// Get JWT token for valid credentials (API version >=2.0).
        /// </summary>
        /// <param name="request">The JWT token request containing AccessKey and AccessSecret.</param>
        /// <returns>JWT token response if credentials are valid; otherwise, an error response.</returns>
        [HttpPost("token")]
        [MapToApiVersion("2.0")]
        [MapToApiVersion("3.0")]
        [SwaggerOperation(
            Description = "Generates a JWT token for valid credentials in API version >=2.0"
        )]
        [ProducesResponseType(typeof(JwtTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult GetToken([FromBody] JwtTokenRequest request)
        {
            _logger.LogInformation("Token request received for AccessKey: {AccessKey}.", request.AccessKey);

            if (request.AccessKey == "demo-key" && request.AccessSecret == "demo-secret")
            {
                _logger.LogInformation("Valid credentials provided.");
                var response = new JwtTokenResponse
                {
                    Token = "generated-jwt-token",
                    ExpiresOn = DateTime.UtcNow.AddMinutes(30)
                };

                return Ok(response);
            }

            _logger.LogWarning("Invalid credentials provided for AccessKey: {AccessKey}.", request.AccessKey);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        /// <summary>
        /// Get an error handled by main error handler.
        /// </summary>
        /// <returns>Error message.</returns>
        [HttpPost("unexpected-error")]
        [MapToApiVersion("3.0")]
        [SwaggerOperation(
            Description = "Handles unexpected errors in API version 3.0"
        )]
        [ProducesResponseType(typeof(JwtTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult GetUnexpectedError([FromBody] JwtTokenRequest request)
        {
            _logger.LogInformation("Error request received for AccessKey: {AccessKey}.", request.AccessKey);
            
            throw new Exception("This is a test exception for error handling.", new Exception("Inner exception for testing purposes."));
        }
    }
}