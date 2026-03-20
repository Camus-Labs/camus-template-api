using System.Data;
using BCrypt.Net;
using Dapper;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// PostgreSQL implementation of user repository using Dapper and BCrypt for password hashing.
/// </summary>
internal sealed class PSUserRepository : IUserRepository
{
    private readonly PSUnitOfWork _unitOfWork;
    private readonly IConnectionFactory _connectionFactory;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSUserRepository"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work for accessing the shared database connection.</param>
    /// <param name="connectionFactory">Factory for creating database connections (used only during initialization).</param>
    public PSUserRepository(
        PSUnitOfWork unitOfWork,
        IConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _unitOfWork = unitOfWork;
        _connectionFactory = connectionFactory;
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
            throw new InvalidOperationException("PSUserRepository already initialized.");
        }

        // Test connection and verify tables exist
        using var connection = _connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();

        const string checkTablesSql = @"
            SELECT
                (SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_schema = 'camus' AND table_name = 'users'
                )) as users_exists,
                (SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_schema = 'camus' AND table_name = 'roles'
                )) as roles_exists,
                (SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_schema = 'camus' AND table_name = 'user_roles'
                )) as user_roles_exists,
                (SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_schema = 'camus' AND table_name = 'role_permissions'
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
        EnsureInitialized();

        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var connection = await _unitOfWork.GetConnectionAsync();

        // Get user with password hash
        const string userSql = @"
            SELECT
                id,
                username,
                password_hash
            FROM camus.users
            WHERE username = @Username";

        var userModel = await connection.QuerySingleOrDefaultAsync<UserModel>(
            userSql,
            new { username });

        if (userModel == null)
        {
            throw new UnauthorizedAccessException(
                "The provided credentials are invalid. User not found.");
        }

        // Verify password against bcrypt hash
        bool isPasswordValid;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(password, userModel.PasswordHash);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to verify password. The password hash may be corrupted.", ex);
        }

        if (!isPasswordValid)
        {
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
            FROM camus.roles r
            INNER JOIN camus.user_roles ur ON r.id = ur.role_id
            LEFT JOIN camus.role_permissions rp ON r.id = rp.role_id
            WHERE ur.user_id = @UserId
            GROUP BY r.id, r.name, r.description
            ORDER BY r.name";

        var roleModels = await connection.QueryAsync<RoleModel>(
            rolesSql,
            new { UserId = userModel.Id });

        return userModel.ToEntity(roleModels);
    }

    /// <summary>
    /// Retrieves a user by their unique identifier with roles.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <returns>The User if found, otherwise null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the repository has not been initialized.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    public async Task<User> GetByIdAsync(Guid userId)
    {
        EnsureInitialized();

        var connection = await _unitOfWork.GetConnectionAsync();

        const string userSql = @"
            SELECT id, username
            FROM camus.users
            WHERE id = @UserId";

        var userModel = await connection.QuerySingleOrDefaultAsync<UserModel>(
            userSql,
            new { UserId = userId })
            ?? throw new KeyNotFoundException($"User with ID '{userId}' not found.");

        const string rolesSql = @"
            SELECT
                r.id,
                r.name,
                r.description,
                ARRAY_AGG(rp.permission) FILTER (WHERE rp.permission IS NOT NULL) as permissions
            FROM camus.roles r
            INNER JOIN camus.user_roles ur ON r.id = ur.role_id
            LEFT JOIN camus.role_permissions rp ON r.id = rp.role_id
            WHERE ur.user_id = @UserId
            GROUP BY r.id, r.name, r.description
            ORDER BY r.name";

        var roleModels = await connection.QueryAsync<RoleModel>(
            rolesSql,
            new { UserId = userModel.Id });

        return userModel.ToEntity(roleModels);
    }

    /// <summary>
    /// Updates the last login timestamp for a user.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task UpdateLastLoginAsync(Guid userId)
    {
        EnsureInitialized();

        var connection = await _unitOfWork.GetConnectionAsync();

        const string updateSql = @"
            UPDATE camus.users
            SET last_login = NOW()
            WHERE id = @UserId";

        var rowsAffected = await connection.ExecuteAsync(updateSql, new { userId });

        if (rowsAffected == 0)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' not found.");
        }
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call Initialize() first.");
        }
    }
}
