namespace emc.camus.application.Auth;

/// <summary>
/// Filter criteria for querying generated tokens.
/// Used alongside PaginationParams to filter and paginate results.
/// </summary>
/// <param name="ExcludeRevoked">When true, excludes revoked tokens from the results.</param>
/// <param name="ExcludeExpired">When true, excludes expired tokens from the results.</param>
public record GeneratedTokenFilter(
    bool ExcludeRevoked = false,
    bool ExcludeExpired = false
);
