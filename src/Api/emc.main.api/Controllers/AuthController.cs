using emc.camus.main.api.Models;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
    
namespace emc.camus.main.api.Controllers
{

    /// <summary>
    /// This is the Authentication controller, normally here you define the get token operation 
    /// and it will take care of the authentication, remember authentication is different from authorization
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        ///  AuthController constructor initializes the controller with configuration, activity source, and logger.
        /// It is used to access application settings, logging, and tracing capabilities.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="activitySource"></param>
        /// <param name="signingCredentials"></param>
        /// <param name="secretProvider"></param>
        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// This is the info method, it provides information about the API and its standards
        /// </summary>
        /// <returns></returns> 
        [HttpGet("info")]
        [MapToApiVersion("1.0")]
        public IActionResult GetInfoV1()
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
        /// This is the info method, it provides information about the API and its standards
        /// </summary>
        /// <returns></returns> 
        [HttpGet("info")]
        [MapToApiVersion("2.0")]
        public IActionResult GetInfoV2()
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
        /// This is the get token method, it receives an AccessKey and AccessSecret in order to provide the token
        /// this token works with an audience and provider, meaning this token only works for this API
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("token")]
        [MapToApiVersion("2.0")]
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
            return Unauthorized("Invalid credentials.");
        }
    }
}