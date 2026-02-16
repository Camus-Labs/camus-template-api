using emc.camus.application.ApiInfo;
using emc.camus.api.Models.Responses;

namespace emc.camus.api.Mapping;

/// <summary>
/// Extension methods for mapping between ApiInfo Application Results and API Response DTOs.
/// </summary>
/// <remarks>
/// Follows the API layer pattern:
/// - Converts Application Results to API Response DTOs
/// - No validation needed for responses (validation happens in Application layer)
/// - Ensures clean separation between Application and API layers
/// </remarks>
public static class ApiInfoMappingExtensions
{
    /// <summary>
    /// Converts an ApiInfoResults (Application layer) to ApiInfoResponse (API layer).
    /// </summary>
    /// <param name="results">The application results to convert</param>
    /// <returns>API response DTO</returns>
    public static ApiInfoResponse ToResponse(this ApiInfoResults results)
    {
        return new ApiInfoResponse
        {
            Version = results.Version,
            Status = results.Status,
            Features = results.Features
        };
    }
}
