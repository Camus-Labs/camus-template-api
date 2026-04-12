using Swashbuckle.AspNetCore.Filters;
using emc.camus.api.Models.Requests.V2;

using System.Diagnostics.CodeAnalysis;

namespace emc.camus.api.SwaggerExamples.V2;

/// <summary>
/// Provides example data for GenerateTokenRequest in Swagger documentation.
/// </summary>
[ExcludeFromCodeCoverage]
public class GenerateTokenRequestExample
    : IExamplesProvider<GenerateTokenRequest>
{
    /// <summary>
    /// Returns an example token generation request for API documentation.
    /// </summary>
    /// <returns>Example request with sample suffix, expiration, and permissions.</returns>
    public GenerateTokenRequest GetExamples()
    {
        return new GenerateTokenRequest
        {
            UsernameSuffix = "ci-deploy",
            ExpiresOn = DateTime.UtcNow.AddDays(30),
            Permissions = new List<string> { "read", "write" }
        };
    }
}
