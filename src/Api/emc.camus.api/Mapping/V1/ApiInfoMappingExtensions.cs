using emc.camus.application.ApiInfo;
using emc.camus.api.Models.Responses.V1;

namespace emc.camus.api.Mapping.V1;

/// <summary>
/// Extension methods for mapping between ApiInfo Application Views and API V1 Response DTOs.
/// </summary>
/// <remarks>
/// Follows the API layer pattern:
/// - Converts Application Views to API Response DTOs
/// - Converts primitives to Application-layer filter types
/// - No validation needed for responses (validation happens in Application layer)
/// - Ensures clean separation between Application and API layers
/// </remarks>
public static class ApiInfoMappingExtensions
{
    /// <summary>
    /// Converts a version string to an ApiInfoFilter (Application layer).
    /// </summary>
    /// <param name="version">The API version string from the route.</param>
    /// <returns>An API info filter for the application layer.</returns>
    public static ApiInfoFilter ToFilter(string version)
    {
        return new ApiInfoFilter(version);
    }

    /// <summary>
    /// Converts an ApiInfoView (Application layer) to ApiInfoResponse (API V1 layer).
    /// </summary>
    /// <param name="view">The application view to convert</param>
    /// <returns>API response DTO</returns>
    public static ApiInfoResponse ToResponse(this ApiInfoView view)
    {
        return new ApiInfoResponse
        {
            Version = view.Version,
            Status = view.Status,
            Features = view.Features
        };
    }
}
