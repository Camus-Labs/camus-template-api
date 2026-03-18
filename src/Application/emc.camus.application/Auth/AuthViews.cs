namespace emc.camus.application.Auth;

/// <summary>
/// Summary view of a generated token for listing purposes (does not include the token value).
/// </summary>
public sealed record GeneratedTokenSummaryView
{
    /// <summary>The JWT ID — unique identifier for the token.</summary>
    public Guid Jti { get; }

    /// <summary>The username associated with the token (includes suffix).</summary>
    public string TokenUsername { get; }

    /// <summary>The permissions granted to this token.</summary>
    public IReadOnlyList<string> Permissions { get; }

    /// <summary>The expiration date and time of the token (UTC).</summary>
    public DateTime ExpiresOn { get; }

    /// <summary>The creation date and time of the token (UTC).</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Whether the token has been revoked.</summary>
    public bool IsRevoked { get; }

    /// <summary>The revocation date and time (UTC), if revoked.</summary>
    public DateTime? RevokedAt { get; }

    /// <summary>Whether the token is currently valid (not expired and not revoked).</summary>
    public bool IsValid { get; }

    /// <summary>
    /// Creates a new generated token summary view.
    /// </summary>
    /// <param name="jti">The JWT ID — unique identifier for the token.</param>
    /// <param name="tokenUsername">The username associated with the token (includes suffix).</param>
    /// <param name="permissions">The permissions granted to this token.</param>
    /// <param name="expiresOn">The expiration date and time of the token (UTC).</param>
    /// <param name="createdAt">The creation date and time of the token (UTC).</param>
    /// <param name="isRevoked">Whether the token has been revoked.</param>
    /// <param name="revokedAt">The revocation date and time (UTC), if revoked.</param>
    /// <param name="isValid">Whether the token is currently valid (not expired and not revoked).</param>
    public GeneratedTokenSummaryView(
        Guid jti,
        string tokenUsername,
        IReadOnlyList<string> permissions,
        DateTime expiresOn,
        DateTime createdAt,
        bool isRevoked,
        DateTime? revokedAt,
        bool isValid)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(jti, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenUsername);
        ArgumentNullException.ThrowIfNull(permissions);
        Jti = jti;
        TokenUsername = tokenUsername;
        Permissions = permissions;
        ExpiresOn = expiresOn;
        CreatedAt = createdAt;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
        IsValid = isValid;
    }
}
