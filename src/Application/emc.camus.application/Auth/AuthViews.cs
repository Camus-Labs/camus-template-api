namespace emc.camus.application.Auth;

/// <summary>
/// Summary view of a generated token for listing purposes (does not include the token value).
/// </summary>
/// <param name="Jti">The JWT ID — unique identifier for the token.</param>
/// <param name="TokenUsername">The username associated with the token (includes suffix).</param>
/// <param name="Permissions">The permissions granted to this token.</param>
/// <param name="ExpiresOn">The expiration date and time of the token (UTC).</param>
/// <param name="CreatedAt">The creation date and time of the token (UTC).</param>
/// <param name="IsRevoked">Whether the token has been revoked.</param>
/// <param name="RevokedAt">The revocation date and time (UTC), if revoked.</param>
/// <param name="IsValid">Whether the token is currently valid (not expired and not revoked).</param>
public record GeneratedTokenSummaryView(
    Guid Jti,
    string TokenUsername,
    List<string> Permissions,
    DateTime ExpiresOn,
    DateTime CreatedAt,
    bool IsRevoked,
    DateTime? RevokedAt,
    bool IsValid
);
