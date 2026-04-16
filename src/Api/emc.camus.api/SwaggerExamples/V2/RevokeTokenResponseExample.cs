using System.Diagnostics.CodeAnalysis;
using Swashbuckle.AspNetCore.Filters;
using emc.camus.api.Models.Dtos.V2;
using emc.camus.api.Models.Responses;

namespace emc.camus.api.SwaggerExamples.V2;

/// <summary>
/// Provides example data for ApiResponse&lt;GeneratedTokenSummaryDto&gt;
/// in Swagger documentation (used by RevokeToken endpoint).
/// </summary>
[ExcludeFromCodeCoverage]
public class RevokeTokenResponseExample
    : IExamplesProvider<ApiResponse<GeneratedTokenSummaryDto>>
{
    /// <summary>
    /// Returns an example revoke token response for API documentation.
    /// </summary>
    /// <returns>Example response with a revoked token summary.</returns>
    public ApiResponse<GeneratedTokenSummaryDto> GetExamples()
    {
        return new ApiResponse<GeneratedTokenSummaryDto>
        {
            Message = "Token revoked successfully",
            Data = new GeneratedTokenSummaryDto
            {
                Jti = Guid.NewGuid(),
                TokenUsername = "testuser-ci-deploy",
                Permissions = new List<string> { "api.read", "api.write" },
                ExpiresOn = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow,
                IsValid = false
            },
            Timestamp = DateTime.UtcNow
        };
    }
}
