using Swashbuckle.AspNetCore.Filters;
using emc.camus.domain.Auth;
using emc.camus.domain.Generic;

namespace emc.camus.api.SwaggerExamples
{
    /// <summary>
    /// Provides example data for ApiResponse&lt;AuthToken&gt; in Swagger documentation.
    /// </summary>
    public class AuthTokenExample : IExamplesProvider<ApiResponse<AuthToken>>
    {
        /// <summary>
        /// Returns an example ApiResponse&lt;AuthToken&gt; object for API documentation.
        /// </summary>
        /// <returns>Example auth token response with sample JWT token.</returns>
        public ApiResponse<AuthToken> GetExamples()
        {
            return new ApiResponse<AuthToken>
            {
                Message = "Token generated successfully",
                Data = new AuthToken
                {
                    Token = "{{JWT_TOKEN_HERE}}",
                    ExpiresOn = DateTime.UtcNow.AddMinutes(60)
                },
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
