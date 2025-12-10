namespace emc.camus.main.api.Controllers
{
    using emc.camus.main.api.Models;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// This is the Authentication controller, normally here you define the get token operation 
    /// and it will take care of the authentication, remember authentication is different from authorization
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            _logger.LogInformation("API info requested.");

            var info = new ApiInfo
            {
                Name = "My Basic API",
                Version = "1.0.0",
                Status = "Running"
            };

            _logger.LogInformation("API info returned successfully.");
            return Ok(info);
        }

        [HttpPost("token")]
        public IActionResult GetToken([FromBody] JwtTokenRequest request)
        {
            _logger.LogInformation("Token request received for AccessKey: {AccessKey}", request.AccessKey);

            if (request.AccessKey == "demo-key" && request.AccessSecret == "demo-secret")
            {
                _logger.LogInformation("Valid credentials provided");
                var response = new JwtTokenResponse
                {
                    Token = "generated-jwt-token",
                    ExpiresOn = DateTime.UtcNow.AddMinutes(30)
                };

                return Ok(response);
            }

            _logger.LogWarning("Invalid credentials provided for AccessKey: {AccessKey}", request.AccessKey);
            return Unauthorized("Invalid credentials");
        }
    }
}