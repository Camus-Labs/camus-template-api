using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.Mapping;

/// <summary>
/// Provides extension methods for mapping between ApiInfoModel (database) and ApiInfo (domain).
/// </summary>
public static class ApiInfoMappingExtensions
{
    /// <summary>
    /// Maps an ApiInfoModel to an ApiInfo domain entity.
    /// </summary>
    /// <param name="model">The database model to map from.</param>
    /// <returns>A new ApiInfo domain entity.</returns>
    public static ApiInfo ToEntity(this ApiInfoModel model)
    {
        return ApiInfo.Reconstitute(
            name: !string.IsNullOrWhiteSpace(model.Name) ? model.Name : "My Basic API",
            version: model.Version,
            status: model.Status,
            features: model.Features?.Count > 0 ? model.Features : new List<string>());
    }
}
