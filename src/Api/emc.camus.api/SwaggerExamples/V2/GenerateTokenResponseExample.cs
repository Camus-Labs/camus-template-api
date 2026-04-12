using Swashbuckle.AspNetCore.Filters;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;

using System.Diagnostics.CodeAnalysis;

namespace emc.camus.api.SwaggerExamples.V2;

/// <summary>
/// Provides example data for ApiResponse&lt;GenerateTokenResponse&gt;
/// in Swagger documentation.
/// </summary>
[ExcludeFromCodeCoverage]
public class GenerateTokenResponseExample
    : IExamplesProvider<ApiResponse<GenerateTokenResponse>>
{
    /// <summary>
    /// Returns an example token generation response for API documentation.
    /// </summary>
    /// <returns>Example response with sample token, expiration, and username.</returns>
    public ApiResponse<GenerateTokenResponse> GetExamples()
    {
        return new ApiResponse<GenerateTokenResponse>
        {
            Message = "Token generated successfully",
            Data = new GenerateTokenResponse
            {
                Token = "{{JWT_TOKEN_HERE}}",
                ExpiresOn = DateTime.UtcNow.AddDays(30),
                TokenUsername = "testuser-ci-deploy"
            },
            Timestamp = DateTime.UtcNow
        };
    }
}
