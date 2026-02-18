using Swashbuckle.AspNetCore.Filters;
using emc.camus.api.Models.Responses;

namespace emc.camus.api.SwaggerExamples
{
    /// <summary>
    /// Provides example data for ApiResponse&lt;AuthenticateUserResponse&gt; in Swagger documentation.
    /// </summary>
    public class AuthTokenExample : IExamplesProvider<ApiResponse<AuthenticateUserResponse>>
    {
        /// <summary>
        /// Returns an example ApiResponse&lt;AuthenticateUserResponse&gt; object for API documentation.
        /// </summary>
        /// <returns>Example authentication response with sample JWT token.</returns>
        public ApiResponse<AuthenticateUserResponse> GetExamples()
        {
            return new ApiResponse<AuthenticateUserResponse>
            {
                Message = "User authenticated successfully",
                Data = new AuthenticateUserResponse
                {
                    Token = "{{JWT_TOKEN_HERE}}",
                    ExpiresOn = DateTime.UtcNow.AddMinutes(60)
                },
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
