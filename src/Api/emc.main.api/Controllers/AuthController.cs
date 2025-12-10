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

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            var apiInfo = new ApiInfo
            {
                Name = "My Basic API",
                Version = "1.0.0",
                Status = "Running"
            };

            return Ok(apiInfo);
        }

        [HttpPost("token")]
        public IActionResult GetToken([FromBody] JwtTokenRequest request)
        {
            if (request.AccessKey == "demo-key" && request.AccessSecret == "demo-secret")
            {
                var response = new JwtTokenResponse
                {
                    Token = "generated-jwt-token",
                    ExpiresOn = DateTime.UtcNow.AddMinutes(30)
                };

                return Ok(response);
            }

            return Unauthorized("Invalid credentials");
        }
    }
}