namespace emc.camus.persistence.postgresql.Models;

/// <summary>
/// Data model representing users table structure in PostgreSQL.
/// Used by Dapper for ORM mapping.
/// </summary>
public class UserModel
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bcrypt password hash for authentication.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
}
