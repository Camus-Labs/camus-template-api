namespace emc.camus.domain.Auth;

/// <summary>
/// Represents a generated token with custom expiration and permissions.
/// Tracks tokens created by authenticated users for audit and security purposes.
/// </summary>
public class GeneratedToken
{
    /// <summary>
    /// Gets the JTI (JWT ID) — the primary identifier for this generated token.
    /// </summary>
    public Guid Jti { get; private set; }

    /// <summary>
    /// Gets the user ID of the user who created this token.
    /// </summary>
    public Guid CreatorUserId { get; private set; }

    /// <summary>
    /// Gets the username of the user who created this token.
    /// </summary>
    public string CreatorUsername { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the username associated with this token (includes suffix).
    /// Format: {CreatorUsername}-{Suffix}
    /// </summary>
    public string TokenUsername { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the permissions granted to this token.
    /// </summary>
    public List<string> Permissions { get; private set; } = new();

    /// <summary>
    /// Gets the expiration date and time of the token (UTC).
    /// </summary>
    public DateTime ExpiresOn { get; private set; }

    /// <summary>
    /// Gets the creation date and time of the token (UTC). Lifecycle field — set by repository.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// Gets the revocation date and time (UTC), if revoked.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Creates a new generated token. Validates business attributes and sets initial state.
    /// </summary>
    /// <param name="jti">The JWT ID — primary identifier for this token.</param>
    /// <param name="creatorUserId">The user ID who created this token.</param>
    /// <param name="creatorUsername">The username who created this token.</param>
    /// <param name="tokenUsername">The username for the token (with suffix).</param>
    /// <param name="permissions">The permissions granted to this token.</param>
    /// <param name="expiresOn">The expiration date and time (UTC).</param>
    public GeneratedToken(
        Guid jti,
        Guid creatorUserId,
        string creatorUsername,
        string tokenUsername,
        List<string> permissions,
        DateTime expiresOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorUsername);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenUsername);
        ArgumentNullException.ThrowIfNull(permissions);

        if (permissions.Count == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(permissions));
        }

        Jti = jti;
        CreatorUserId = creatorUserId;
        CreatorUsername = creatorUsername;
        TokenUsername = tokenUsername;
        Permissions = permissions;
        ExpiresOn = expiresOn;
        IsRevoked = false;
    }

    /// <summary>
    /// Private constructor for reconstitution from persistence.
    /// </summary>
    private GeneratedToken() { }

    /// <summary>
    /// Rebuilds a generated token from persistence data. Skips business validation
    /// since data is already validated. Populates all fields including lifecycle fields.
    /// </summary>
    public static GeneratedToken Reconstitute(
        Guid jti,
        Guid creatorUserId,
        string creatorUsername,
        string tokenUsername,
        List<string> permissions,
        DateTime expiresOn,
        DateTime createdAt,
        bool isRevoked,
        DateTime? revokedAt)
    {
        return new GeneratedToken
        {
            Jti = jti,
            CreatorUserId = creatorUserId,
            CreatorUsername = creatorUsername,
            TokenUsername = tokenUsername,
            Permissions = permissions,
            ExpiresOn = expiresOn,
            CreatedAt = createdAt,
            IsRevoked = isRevoked,
            RevokedAt = revokedAt
        };
    }

    /// <summary>
    /// Marks the token as revoked. Enforces invariant: a token can only be revoked once.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the token is already revoked.</exception>
    public void Revoke()
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Token is already revoked.");
        }

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the token is currently valid (not expired and not revoked).
    /// </summary>
    /// <returns>True if the token is valid, false otherwise.</returns>
    public bool IsValid()
    {
        return !IsRevoked && ExpiresOn > DateTime.UtcNow;
    }
}
