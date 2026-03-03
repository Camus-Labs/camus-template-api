namespace emc.camus.api.Models.Requests;

/// <summary>
/// Query parameters for the GET tokens endpoint with filtering and pagination.
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
}
