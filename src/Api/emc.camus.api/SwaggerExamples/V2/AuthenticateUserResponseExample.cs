using Swashbuckle.AspNetCore.Filters;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;

namespace emc.camus.api.SwaggerExamples.V2;

/// <summary>
/// Provides example data for ApiResponse&lt;AuthenticateUserResponse&gt;
/// in Swagger documentation.
/// </summary>
public class AuthenticateUserResponseExample
    : IExamplesProvider<ApiResponse<AuthenticateUserResponse>>
{
    /// <summary>
    /// Returns an example authentication response for API documentation.
    /// </summary>
    /// <returns>Example response with sample JWT token.</returns>
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
