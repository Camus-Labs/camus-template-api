namespace emc.camus.application.Auth;

/// <summary>
/// Filter criteria for querying generated tokens.
/// Used alongside PaginationParams to filter and paginate results.
/// </summary>
public sealed record GeneratedTokenFilter
{
    /// <summary>When true, excludes revoked tokens from the results.</summary>
    public bool ExcludeRevoked { get; }

    /// <summary>When true, excludes expired tokens from the results.</summary>
    public bool ExcludeExpired { get; }

    /// <summary>
    /// Creates a new generated token filter.
    /// </summary>
    /// <param name="excludeRevoked">When true, excludes revoked tokens from the results.</param>
    /// <param name="excludeExpired">When true, excludes expired tokens from the results.</param>
    public GeneratedTokenFilter(bool excludeRevoked = false, bool excludeExpired = false)
    {
        ExcludeRevoked = excludeRevoked;
        ExcludeExpired = excludeExpired;
    }
}
