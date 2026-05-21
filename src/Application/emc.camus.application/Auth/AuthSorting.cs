using emc.camus.application.Common;

namespace emc.camus.application.Auth;

/// <summary>
/// Specifies the field by which generated tokens can be sorted.
/// </summary>
public enum GeneratedTokenSortField
{
    /// <summary>Sort by token username.</summary>
    TokenUsername,

    /// <summary>Sort by expiration date.</summary>
    ExpiresOn,

    /// <summary>Sort by creation date.</summary>
    CreatedAt,

    /// <summary>Sort by revocation date.</summary>
    RevokedAt
}
