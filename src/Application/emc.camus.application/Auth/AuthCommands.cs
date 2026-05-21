using emc.camus.domain.Auth;

namespace emc.camus.application.Auth;

/// <summary>
/// Command to authenticate a user with username and password.
/// </summary>
public sealed record AuthenticateUserCommand
{
    /// <summary>The username for authentication.</summary>
    public string Username { get; }

    /// <summary>The password for authentication.</summary>
    public string Password { get; }

    /// <summary>
    /// Creates a new authentication command.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    public AuthenticateUserCommand(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        Username = username;
        Password = password;
        ValidateUsername();
    }

    private void ValidateUsername()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Username);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Username.Length, User.MaxUsernameLength, nameof(Username));
    }
}

/// <summary>
/// Command to generate a custom token with specific permissions and expiration.
/// </summary>
public sealed record GenerateTokenCommand
{
    /// <summary>The suffix to append to the current username (up to 20 chars, alphanumeric + . - _ only).</summary>
    public string UsernameSuffix { get; }

    /// <summary>The custom expiration date (UTC). Must be between 1 hour and 1 year from now.</summary>
    public DateTime ExpiresOn { get; }

    /// <summary>The list of permissions to grant to the token. Must be a subset of the current user's permissions.</summary>
    public IReadOnlyList<string> Permissions { get; }

    /// <summary>
    /// Creates a new token generation command.
    /// </summary>
    /// <param name="usernameSuffix">The suffix to append to the current username.</param>
    /// <param name="expiresOn">The custom expiration date (UTC).</param>
    /// <param name="permissions">The list of permissions to grant to the token.</param>
    /// <exception cref="ArgumentException">Thrown when usernameSuffix is null/empty or permissions contain invalid values.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when usernameSuffix exceeds max length, expiresOn is default, or permissions is empty.</exception>
    public GenerateTokenCommand(string usernameSuffix, DateTime expiresOn, List<string> permissions)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(expiresOn, default);
        ArgumentNullException.ThrowIfNull(permissions);
        UsernameSuffix = usernameSuffix;
        ExpiresOn = expiresOn;
        Permissions = permissions.ToList().AsReadOnly();
        ValidateUsernameSuffix();
        ValidatePermissions();
    }

    private void ValidateUsernameSuffix()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(UsernameSuffix);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(UsernameSuffix.Length, GeneratedToken.MaxSuffixLength, nameof(UsernameSuffix));

        if (!UsernameSuffix.All(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_'))
        {
            throw new ArgumentException(
                $"UsernameSuffix '{UsernameSuffix}' contains invalid characters. Only alphanumeric, '.', '-', and '_' are allowed.",
                nameof(UsernameSuffix));
        }
    }

    private void ValidatePermissions()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Permissions.Count, 1, nameof(Permissions));

        var validPermissions = Auth.Permissions.GetAll();
        var invalidPermissions = Permissions.Where(p => !validPermissions.Contains(p)).ToList();

        if (invalidPermissions.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid permissions: {string.Join(", ", invalidPermissions)}. Valid permissions are: {string.Join(", ", validPermissions)}.",
                nameof(Permissions));
        }
    }
}

/// <summary>
/// Command to revoke a generated token by its JTI (JWT ID).
/// </summary>
public sealed record RevokeTokenCommand
{
    /// <summary>The JWT ID of the token to revoke.</summary>
    public Guid Jti { get; }

    /// <summary>
    /// Creates a new token revocation command.
    /// </summary>
    /// <param name="jti">The JWT ID of the token to revoke.</param>
    public RevokeTokenCommand(Guid jti)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(jti, Guid.Empty);
        Jti = jti;
    }
}
