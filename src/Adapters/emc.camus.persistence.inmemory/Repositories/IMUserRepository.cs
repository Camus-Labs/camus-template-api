using System.Data;
using emc.camus.application.Auth;
using emc.camus.application.Configurations;
using emc.camus.application.Common;
using emc.camus.application.Secrets;
using emc.camus.domain.Auth;
using Microsoft.Extensions.Logging;

namespace emc.camus.persistence.inmemory.Repositories;

/// <summary>
/// In-memory implementation of user repository that loads configuration from settings.
/// This is a temporary implementation for development/testing. In production, replace with database implementation.
/// </summary>
public partial class IMUserRepository : IUserRepository
{
    private readonly InMemoryAuthorizationSettings _settings;
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<IMUserRepository> _logger;
    private List<Role> _roles = new();
    private Dictionary<string, (User User, string PasswordSecretName)> _usersByUsername = new();
    private bool _initialized;

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "IMUserRepository already initialized. Skipping.")]
    private partial void LogAlreadyInitialized();

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "User not found for username: {Username}")]
    private partial void LogUserNotFound(string username);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Invalid password for user: {Username}")]
    private partial void LogInvalidPassword(string username);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Authentication successful for user: {Username}")]
    private partial void LogAuthenticationSuccessful(string username);

    /// <summary>
    /// Initializes a new instance of the <see cref="IMUserRepository"/> class.
    /// </summary>
    /// <param name="settings">Authorization settings containing role and user definitions.</param>
    /// <param name="secretProvider">Provider for retrieving stored secrets.</param>
    /// <param name="logger">Logger for repository events.</param>
    public IMUserRepository(
        AuthorizationSettings settings,
        ISecretProvider secretProvider,
        ILogger<IMUserRepository> logger)
    {
        _logger = logger;
        _settings = settings.InMemory;
        _secretProvider = secretProvider;
    }

    /// <summary>
    /// Initializes the in-memory repository by loading roles and users from configuration settings
    /// and retrieving their credentials from the secret provider. This method must be called once
    /// at application startup to populate the in-memory store before any authentication attempts.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any username or password secret cannot be retrieved from the secret store.
    /// </exception>
    public void Initialize()
    {
        if (_initialized)
        {
            LogAlreadyInitialized();
            return;
        }

        // Load roles from configuration
        _roles = _settings.Roles.Select(roleConfig =>
            new Role(
                roleConfig.Name,
                null, // Description removed from RoleSettings
                roleConfig.Permissions,
                null // Auto-generate ID
            )
        ).ToList();

        // Load users from configuration and index by username for fast lookup
        _usersByUsername = new Dictionary<string, (User, string)>();

        foreach (var userConfig in _settings.Users)
        {
            var userRoles = userConfig.Roles
                .Select(roleName => _roles.First(r => r.Name == roleName))
                .ToList();

            // Get actual username from secret store
            var username = _secretProvider.GetSecret(userConfig.UsernameSecretName);

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException($"Failed to retrieve username from secret '{userConfig.UsernameSecretName}'. Ensure the secret exists in the secret store.");
            }

            // Validate password secret exists
            var password = _secretProvider.GetSecret(userConfig.PasswordSecretName);
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException($"Failed to retrieve password from secret '{userConfig.PasswordSecretName}'. Ensure the secret exists in the secret store.");
            }

            var user = new User(
                username,
                userRoles,
                null // Auto-generate ID
            );

            _usersByUsername[username] = (user, userConfig.PasswordSecretName);
        }

        _initialized = true;
    }

    /// <summary>
    /// Validates user credentials by looking up the username in the in-memory store and comparing
    /// the password with the value retrieved from the secret provider (connection parameter ignored).
    /// </summary>
    /// <param name="connection">The database connection (ignored for in-memory implementation).</param>
    /// <param name="username">The username to validate.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns>The authenticated user with roles.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when credentials are invalid (empty, user not found, or wrong password).
    /// </exception>
    public Task<User> ValidateCredentialsAsync(IDbConnection connection, string username, string password)
    {
        EnsureInitialized();

        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        // Find user by username (O(1) lookup)
        if (!_usersByUsername.TryGetValue(username, out var userEntry))
        {
            LogUserNotFound(username);
            throw new UnauthorizedAccessException("The provided credentials are invalid. User not found.");
        }

        // Get password from secret store - is guarantee to exist in secretProvider because Initialize() loads this at startup and throws if any secrets are missing
        var passwordFromSecret = _secretProvider.GetSecret(userEntry.PasswordSecretName);

        // Validate password
        // Note: In production, this should use secure password hashing (bcrypt, Argon2, etc.)
        if (passwordFromSecret != password)
        {
            LogInvalidPassword(userEntry.User.Username);
            throw new UnauthorizedAccessException("The provided credentials are invalid. Username and password mismatch.");
        }

        LogAuthenticationSuccessful(userEntry.User.Username);

        return Task.FromResult(userEntry.User);
    }

    /// <summary>
    /// Retrieves a user by their unique identifier from the in-memory store (connection parameter ignored).
    /// </summary>
    /// <param name="connection">The database connection (ignored for in-memory implementation).</param>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <returns>The User with roles.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the repository has not been initialized.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    public Task<User> GetByIdAsync(IDbConnection connection, Guid userId)
    {
        EnsureInitialized();

        var user = _usersByUsername.Values
            .Where(entry => entry.User.Id == userId)
            .Select(entry => entry.User)
            .FirstOrDefault()
            ?? throw new KeyNotFoundException($"User with ID '{userId}' not found.");

        return Task.FromResult(user);
    }

    /// <summary>
    /// Updates the last login timestamp for a user (no-op for in-memory implementation).
    /// </summary>
    /// <param name="connection">The database connection (ignored for in-memory implementation).</param>
    /// <param name="userId">The ID of the user to update.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public Task UpdateLastLoginAsync(IDbConnection connection, Guid userId)
    {
        // In-memory implementation doesn't persist last login timestamp
        return Task.CompletedTask;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call Initialize() first.");
        }
    }
}
