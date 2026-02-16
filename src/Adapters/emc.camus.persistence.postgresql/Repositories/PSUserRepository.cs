using System.Data;
using BCrypt.Net;
using Dapper;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;
using Microsoft.Extensions.Logging;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// PostgreSQL implementation of user repository using Dapper and BCrypt for password hashing.
/// </summary>
public class PSUserRepository : IUserRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<PSUserRepository> _logger;
    private bool _initialized = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSUserRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for creating database connections.</param>
    /// <param name="logger">Logger for repository events.</param>
    public PSUserRepository(
        IConnectionFactory connectionFactory,
        ILogger<PSUserRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the PostgreSQL repository by validating the database connection and schema.
    /// This method must be called once at application startup to verify database connectivity.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when database connection fails or required tables don't exist.
    /// </exception>
    public void Initialize()
    {
        if (_initialized)
        {
            _logger.LogWarning("PSUserRepository already initialized. Skipping.");
            return;
        }

        try
        {
            // Test connection and verify tables exist
            using var connection = _connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            
            const string checkTablesSql = @"
                SELECT 
                    (SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' AND table_name = 'users'
                    )) as users_exists,
                    (SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' AND table_name = 'roles'
                    )) as roles_exists,
                    (SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' AND table_name = 'user_roles'
                    )) as user_roles_exists,
                    (SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' AND table_name = 'role_permissions'
                    )) as role_permissions_exists";
            
            var result = connection.QuerySingle<dynamic>(checkTablesSql);
            
            if (!result.users_exists || !result.roles_exists || 
                !result.user_roles_exists || !result.role_permissions_exists)
            {
                var missingTables = new List<string>();
                if (!result.users_exists) missingTables.Add("users");
                if (!result.roles_exists) missingTables.Add("roles");
                if (!result.user_roles_exists) missingTables.Add("user_roles");
                if (!result.role_permissions_exists) missingTables.Add("role_permissions");
                
                throw new InvalidOperationException(
                    $"Required tables do not exist in the database: {string.Join(", ", missingTables)}. " +
                    "Please run database migrations to create the schema.");
            }

            _initialized = true;
            _logger.LogInformation("PSUserRepository initialized successfully");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to initialize PSUserRepository");
            throw new InvalidOperationException(
                "Failed to initialize user repository. Ensure the database is accessible.", ex);
        }
    }

    /// <summary>
    /// Validates user credentials by looking up the username in the database and verifying 
    /// the password against the stored bcrypt hash.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns>The authenticated user with roles.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when credentials are invalid (empty, user not found, or wrong password).
    /// </exception>
    public async Task<User> ValidateCredentialsAsync(string username, string password)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await ValidateCredentialsAsync(connection, username, password);
    }

    /// <summary>
    /// Validates user credentials by looking up the username in the database and verifying 
    /// the password against the stored bcrypt hash using an external connection (for transactions).
    /// </summary>
    /// <param name="connection">The database connection to use for the operation.</param>
    /// <param name="username">The username to validate.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns>The authenticated user with roles.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when credentials are invalid (empty, user not found, or wrong password).
    /// </exception>
    public async Task<User> ValidateCredentialsAsync(IDbConnection connection, string username, string password)
    {
        EnsureInitialized();

        // Validate input
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Invalid credentials: Username or Password is empty");
            throw new UnauthorizedAccessException(
                "The provided credentials are invalid. Username and password must be provided.");
        }

        // Get user with password hash
        const string userSql = @"
            SELECT 
                id,
                username,
                password_hash
            FROM users
            WHERE username = @Username";

        var userModel = await connection.QuerySingleOrDefaultAsync<UserModel>(
            userSql,
            new { Username = username });

        if (userModel == null)
        {
            _logger.LogWarning("User not found for username: {Username}", username);
            throw new UnauthorizedAccessException(
                "The provided credentials are invalid. User not found.");
        }

        // Verify password against bcrypt hash
        bool isPasswordValid = false;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(password, userModel.PasswordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify password hash for user: {Username}", username);
            throw new InvalidOperationException(
                "Failed to verify password. The password hash may be corrupted.", ex);
        }

        if (!isPasswordValid)
        {
            _logger.LogWarning("Invalid password for user: {Username}", username);
            throw new UnauthorizedAccessException(
                "The provided credentials are invalid. Username and password mismatch.");
        }

        // Get user's roles with permissions
        const string rolesSql = @"
            SELECT 
                r.id,
                r.name,
                r.description,
                ARRAY_AGG(rp.permission) FILTER (WHERE rp.permission IS NOT NULL) as permissions
            FROM roles r
            INNER JOIN user_roles ur ON r.id = ur.role_id
            LEFT JOIN role_permissions rp ON r.id = rp.role_id
            WHERE ur.user_id = @UserId
            GROUP BY r.id, r.name, r.description
            ORDER BY r.name";

        var roleModels = await connection.QueryAsync<RoleModel>(
            rolesSql,
            new { UserId = userModel.Id });

        _logger.LogInformation("Authentication successful for user: {Username}", username);

        return userModel.ToEntity(roleModels);
    }

    /// <summary>
    /// Updates the last login timestamp for a user.
    /// </summary>
    /// <param name="connection">The database connection to use for the operation.</param>
    /// <param name="userId">The ID of the user to update.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task UpdateLastLoginAsync(IDbConnection connection, string userId)
    {
        const string updateSql = @"
            UPDATE users 
            SET last_login = NOW() 
            WHERE id = @UserId";

        await connection.ExecuteAsync(updateSql, new { UserId = userId });
        _logger.LogDebug("Updated last login timestamp for user: {UserId}", userId);
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call Initialize() first.");
        }
    }
}
