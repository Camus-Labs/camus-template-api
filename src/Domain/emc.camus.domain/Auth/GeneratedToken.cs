using System.Text.RegularExpressions;

namespace emc.camus.domain.Auth;

/// <summary>
/// Represents a generated token with custom expiration and permissions.
/// Tracks tokens created by authenticated users for audit and security purposes.
/// </summary>
public class GeneratedToken
{
    private const int MaxSuffixLength = 20;
    private static readonly Regex SuffixPattern = new(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);

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
    public IReadOnlyList<string> Permissions { get; private set; } = new List<string>();

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
    /// Enforces invariants: suffix format/length, expiration range, token permissions must be a subset of the creator's permissions.
    /// </summary>
    /// <param name="creator">The user who is creating this token.</param>
    /// <param name="suffix">The suffix to append to the creator's username (up to 20 chars, alphanumeric + . - _ only).</param>
    /// <param name="permissions">The permissions granted to this token.</param>
    /// <param name="expiresOn">The expiration date and time (UTC). Must be between 1 hour and 1 year from now.</param>
    /// <param name="jti">Optional JTI (JWT ID). If not provided, a new GUID will be generated.</param>
    /// <exception cref="ArgumentNullException">Thrown when creator is null.</exception>
    /// <exception cref="ArgumentException">Thrown when suffix is null/empty or contains invalid characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when suffix exceeds max length, jti is empty, permissions is empty, or expiresOn is outside the valid range.</exception>
    /// <exception cref="InvalidOperationException">Thrown when requested permissions are not a subset of the creator's permissions.</exception>
    public GeneratedToken(
        User creator,
        string suffix,
        List<string> permissions,
        DateTime expiresOn,
        Guid? jti = null)
    {
        ArgumentNullException.ThrowIfNull(creator);
        ValidateSuffix(suffix);
        ValidatePermissions(permissions);
        ValidatePermissionSubset(permissions, creator);
        ValidateExpirationDate(expiresOn);

        if (jti.HasValue)
            ArgumentOutOfRangeException.ThrowIfEqual(jti.Value, Guid.Empty);

        Jti = jti ?? Guid.NewGuid();
        CreatorUserId = creator.Id;
        CreatorUsername = creator.Username;
        TokenUsername = $"{creator.Username}-{suffix}";
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
    /// <param name="jti">The JTI (JWT ID).</param>
    /// <param name="creatorUserId">The user ID of the creator.</param>
    /// <param name="creatorUsername">The username of the creator.</param>
    /// <param name="tokenUsername">The username associated with this token.</param>
    /// <param name="permissions">The permissions granted to this token.</param>
    /// <param name="expiresOn">The expiration date and time (UTC).</param>
    /// <param name="createdAt">The creation date and time (UTC).</param>
    /// <param name="isRevoked">Whether the token has been revoked.</param>
    /// <param name="revokedAt">The revocation date and time (UTC), if revoked.</param>
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
    /// Marks the token as revoked. Enforces invariants: only the creator can revoke,
    /// and a token can only be revoked once.
    /// </summary>
    /// <param name="actingUserId">The ID of the user attempting to revoke the token.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the acting user is not the creator of the token.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the token is already revoked.</exception>
    public void Revoke(Guid actingUserId)
    {
        if (actingUserId != CreatorUserId)
        {
            throw new UnauthorizedAccessException($"User '{actingUserId}' cannot revoke token '{Jti}' — creator is '{CreatorUserId}'.");
        }

        if (IsRevoked)
        {
            throw new InvalidOperationException($"Token {Jti} is already revoked.");
        }

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the token is currently active (not expired and not revoked).
    /// </summary>
    /// <returns>True if the token is active, false otherwise.</returns>
    public bool IsActive()
    {
        return !IsRevoked && ExpiresOn > DateTime.UtcNow;
    }

    /// <summary>
    /// Validates that the expiration date is within the allowed range (1 hour to 1 year from now).
    /// </summary>
    /// <param name="expiresOn">The expiration date to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when expiresOn is outside the valid range.</exception>
    private static void ValidateExpirationDate(DateTime expiresOn)
    {
        var now = DateTime.UtcNow;
        var minExpiration = now.AddHours(1);
        var maxExpiration = now.AddYears(1);

        ArgumentOutOfRangeException.ThrowIfLessThan(expiresOn, minExpiration);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(expiresOn, maxExpiration);
    }

    /// <summary>
    /// Validates that the permissions list is not null and not empty.
    /// </summary>
    /// <param name="permissions">The permissions list to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when permissions is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when permissions is empty.</exception>
    private static void ValidatePermissions(List<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        ArgumentOutOfRangeException.ThrowIfZero(permissions.Count);
    }

    /// <summary>
    /// Validates that the suffix is non-empty, within length limits, and contains only allowed characters.
    /// </summary>
    /// <param name="suffix">The suffix to validate.</param>
    /// <exception cref="ArgumentException">Thrown when suffix is null/empty or contains invalid characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when suffix exceeds max length.</exception>
    private static void ValidateSuffix(string suffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suffix);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(suffix.Length, MaxSuffixLength, nameof(suffix));

        if (!SuffixPattern.IsMatch(suffix))
        {
            throw new ArgumentException(
                $"Suffix can only contain alphanumeric characters, dots (.), hyphens (-), and underscores (_). Got: '{suffix}'.",
                nameof(suffix));
        }
    }

    /// <summary>
    /// Validates that the requested permissions are a subset of the creator's permissions.
    /// </summary>
    /// <param name="permissions">The permissions to grant to this token.</param>
    /// <param name="creator">The user creating this token.</param>
    /// <exception cref="InvalidOperationException">Thrown when the creator does not possess one or more of the requested permissions.</exception>
    private static void ValidatePermissionSubset(List<string> permissions, User creator)
    {
        var creatorPermissions = creator.GetPermissions();
        var unauthorizedPermissions = permissions.Where(p => !creatorPermissions.Contains(p)).ToList();

        if (unauthorizedPermissions.Count > 0)
        {
            throw new InvalidOperationException(
                $"User '{creator.Username}' cannot grant permissions they don't have: {string.Join(", ", unauthorizedPermissions)}.");
        }
    }
}
