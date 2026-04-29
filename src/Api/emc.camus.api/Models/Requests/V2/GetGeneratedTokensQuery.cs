using emc.camus.api.Models.Requests;

namespace emc.camus.api.Models.Requests.V2;

/// <summary>
/// Query parameters for the GET tokens endpoint with filtering, sorting, and pagination.
/// </summary>
public class GetGeneratedTokensQuery : PaginationQuery
{
    /// <summary>
    /// When true, excludes revoked tokens from the results. Defaults to false.
    /// </summary>
    public bool ExcludeRevoked { get; set; }

    /// <summary>
    /// When true, excludes expired tokens from the results. Defaults to false.
    /// </summary>
    public bool ExcludeExpired { get; set; }

    /// <summary>
    /// The field to sort results by. Allowed values: tokenUsername, expiresOn, createdAt, revokedAt.
    /// Must be provided together with <see cref="SortDirection"/>.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// The direction to sort results. Allowed values: asc, desc.
    /// Must be provided together with <see cref="SortBy"/>.
    /// </summary>
    public string? SortDirection { get; set; }
}
