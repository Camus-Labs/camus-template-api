using Swashbuckle.AspNetCore.Filters;
using emc.camus.api.Models.Dtos.V2;
using emc.camus.api.Models.Responses;

using System.Diagnostics.CodeAnalysis;

namespace emc.camus.api.SwaggerExamples.V2;

/// <summary>
/// Provides example data for ApiResponse&lt;PagedResult&lt;GeneratedTokenSummaryResponse&gt;&gt;
/// in Swagger documentation.
/// </summary>
[ExcludeFromCodeCoverage]
public class GeneratedTokenSummaryPagedExample
    : IExamplesProvider<ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>>
{
    /// <summary>
    /// Returns an example paginated token list response for API documentation.
    /// Demonstrates results sorted by createdAt descending (most recent first).
    /// </summary>
    /// <returns>Example response with sample token summaries, pagination metadata, and sort order applied.</returns>
    public ApiResponse<PagedResponse<GeneratedTokenSummaryDto>> GetExamples()
    {
        return new ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>
        {
            Message = "Generated tokens retrieved successfully",
            Data = new PagedResponse<GeneratedTokenSummaryDto>
            {
                Items = new List<GeneratedTokenSummaryDto>
                {
                    new()
                    {
                        Jti = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                        TokenUsername = "testuser-ci-deploy",
                        Permissions = new List<string> { "api.read", "api.write" },
                        ExpiresOn = new DateTime(2026, 2, 14, 12, 0, 0, DateTimeKind.Utc),
                        CreatedAt = new DateTime(2026, 1, 14, 12, 0, 0, DateTimeKind.Utc),
                        IsRevoked = false,
                        RevokedAt = null,
                        IsValid = true
                    },
                    new()
                    {
                        Jti = new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                        TokenUsername = "testuser-staging",
                        Permissions = new List<string> { "api.read" },
                        ExpiresOn = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc),
                        CreatedAt = new DateTime(2025, 12, 16, 12, 0, 0, DateTimeKind.Utc),
                        IsRevoked = true,
                        RevokedAt = new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc),
                        IsValid = false
                    }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 25,
                TotalPages = 1,
                HasNextPage = false,
                HasPreviousPage = false
            },
            Timestamp = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
        };
    }
}
