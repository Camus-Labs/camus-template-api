namespace emc.camus.api.Models.Dtos;

/// <summary>
/// Data transfer object containing a summary of a generated token (without the token value).
/// Used as an item within paginated responses.
/// </summary>
public class GeneratedTokenSummaryDto
{
    /// <summary>
    /// The JWT ID — unique identifier for the token.
    /// </summary>
    public Guid Jti { get; set; }

    /// <summary>
    /// The username associated with the token (includes suffix).
    /// </summary>
    public string TokenUsername { get; set; } = string.Empty;

    /// <summary>
    /// The permissions granted to this token.
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// The expiration date and time of the token (UTC).
    /// </summary>
    public DateTime ExpiresOn { get; set; }

    /// <summary>
    /// The creation date and time of the token (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// The revocation date and time (UTC), if revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Whether the token is currently valid (not expired and not revoked).
    /// </summary>
    public bool IsValid { get; set; }
}
