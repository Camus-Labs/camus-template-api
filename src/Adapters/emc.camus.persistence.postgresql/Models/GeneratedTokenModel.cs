using System.Diagnostics.CodeAnalysis;

namespace emc.camus.persistence.postgresql.Models;

/// <summary>
/// Data model representing generated_tokens table structure in PostgreSQL.
/// Used by Dapper for ORM mapping.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class GeneratedTokenModel
{
    /// <summary>
    /// Gets or sets the JWT ID — primary identifier for the token.
    /// </summary>
    public Guid Jti { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the token creator.
    /// </summary>
    public Guid CreatorUserId { get; set; }

    /// <summary>
    /// Gets or sets the username of the token creator.
    /// </summary>
    public string CreatorUsername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username associated with the token (includes suffix).
    /// </summary>
    public string TokenUsername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permissions granted to this token.
    /// Maps to PostgreSQL text array.
    /// </summary>
    public string[]? Permissions { get; set; }

    /// <summary>
    /// Gets or sets the expiration date and time of the token (UTC).
    /// </summary>
    public DateTime ExpiresOn { get; set; }

    /// <summary>
    /// Gets or sets the creation date and time of the token (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the revocation date and time (UTC), if revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }
}
