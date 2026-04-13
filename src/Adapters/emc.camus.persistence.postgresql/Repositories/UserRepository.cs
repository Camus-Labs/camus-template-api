using BCrypt.Net;
using emc.camus.application.Auth;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Services;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// PostgreSQL implementation of user repository using BCrypt for password hashing.
/// Delegates raw SQL execution to <see cref="IUserDataAccess"/>.
/// </summary>
internal sealed class UserRepository : IUserRepository
{
    private readonly UnitOfWork _unitOfWork;
    private readonly InitializationState _initState;
    private readonly IUserDataAccess _dataAccess;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work for accessing the shared database connection.</param>
    /// <param name="initState">Container-scoped initialization state shared across scoped instances.</param>
    /// <param name="dataAccess">Data access layer for raw SQL execution.</param>
    public UserRepository(
        UnitOfWork unitOfWork,
        InitializationState initState,
        IUserDataAccess dataAccess)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(initState);
        ArgumentNullException.ThrowIfNull(dataAccess);

        _unitOfWork = unitOfWork;
        _initState = initState;
        _dataAccess = dataAccess;
    }

    /// <summary>
    /// Initializes the PostgreSQL repository by validating the database connection and schema.
    /// This method must be called once at application startup to verify database connectivity.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when database connection fails or required tables don't exist.
    /// </exception>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initState.UserRepositoryInitialized)
        {
            throw new InvalidOperationException("UserRepository already initialized.");
        }

        var connection = await _unitOfWork.GetConnectionAsync(ct);
        var tableStatus = await _dataAccess.CheckRequiredTablesAsync(connection, ct);

        string[] requiredTables = ["users", "roles", "user_roles", "role_permissions"];
        var missingTables = requiredTables
            .Where(table => !tableStatus.TryGetValue(table, out var exists) || !exists)
            .ToList();

        if (missingTables.Count > 0)
        {
            throw new InvalidOperationException(
                $"Required tables do not exist in the database: {string.Join(", ", missingTables)}. " +
                "Please run database migrations to create the schema.");
        }

        _initState.UserRepositoryInitialized = true;
    }

    /// <summary>
    /// Validates user credentials by looking up the username in the database and verifying
    /// the password against the stored bcrypt hash.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <param name="password">The password to validate.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The authenticated user with roles.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when credentials are invalid (empty, user not found, or wrong password).
    /// </exception>
    public async Task<User> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        EnsureInitialized();

        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var connection = await _unitOfWork.GetConnectionAsync(ct);

        var userModel = await _dataAccess.FindByUsernameWithHashAsync(connection, username, ct)
            ?? throw new UnauthorizedAccessException(
                "The provided credentials are invalid. User not found.");

        VerifyPassword(password, userModel.PasswordHash);

        var roleModels = await _dataAccess.GetRolesByUserIdAsync(connection, userModel.Id, ct);

        return userModel.ToEntity(roleModels);
    }

    /// <summary>
    /// Retrieves a user by their unique identifier with roles.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The User if found, otherwise null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the repository has not been initialized.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the user is not found.</exception>
    public async Task<User> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        EnsureInitialized();

        var connection = await _unitOfWork.GetConnectionAsync(ct);

        var userModel = await _dataAccess.FindByIdAsync(connection, userId, ct)
            ?? throw new KeyNotFoundException($"User with ID '{userId}' not found.");

        var roleModels = await _dataAccess.GetRolesByUserIdAsync(connection, userModel.Id, ct);

        return userModel.ToEntity(roleModels);
    }

    /// <summary>
    /// Updates the last login timestamp for a user.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task UpdateLastLoginAsync(Guid userId, CancellationToken ct = default)
    {
        EnsureInitialized();

        var connection = await _unitOfWork.GetConnectionAsync(ct);

        var rowsAffected = await _dataAccess.UpdateLastLoginAsync(connection, userId, ct);

        if (rowsAffected == 0)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' not found.");
        }
    }

    private static void VerifyPassword(string password, string passwordHash)
    {
        bool isPasswordValid;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);
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
    }

    private void EnsureInitialized()
    {
        if (!_initState.UserRepositoryInitialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync() first.");
        }
    }
}
