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
                Jti = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                TokenUsername = "testuser-ci-deploy",
                Permissions = new List<string> { "api.read", "api.write" },
                ExpiresOn = new DateTime(2026, 2, 14, 12, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 1, 14, 12, 0, 0, DateTimeKind.Utc),
                IsRevoked = true,
                RevokedAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
                IsValid = false
            },
            Timestamp = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
        };
    }
}
