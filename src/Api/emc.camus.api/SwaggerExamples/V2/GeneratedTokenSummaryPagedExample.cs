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
    /// </summary>
    /// <returns>Example response with sample token summaries and pagination metadata.</returns>
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
                        Jti = Guid.NewGuid(),
                        TokenUsername = "testuser-ci-deploy",
                        Permissions = new List<string> { "read", "write" },
                        ExpiresOn = DateTime.UtcNow.AddDays(30),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        IsRevoked = false,
                        RevokedAt = null,
                        IsValid = true
                    },
                    new()
                    {
                        Jti = Guid.NewGuid(),
                        TokenUsername = "testuser-staging",
                        Permissions = new List<string> { "read" },
                        ExpiresOn = DateTime.UtcNow.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        IsRevoked = true,
                        RevokedAt = DateTime.UtcNow.AddDays(-10),
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
            Timestamp = DateTime.UtcNow
        };
    }
}
