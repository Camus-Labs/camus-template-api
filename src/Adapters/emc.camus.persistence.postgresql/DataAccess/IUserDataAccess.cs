using System.Data;
using emc.camus.persistence.postgresql.Models;

namespace emc.camus.persistence.postgresql.DataAccess;

/// <summary>
/// Thin data access layer for user-related SQL operations.
/// </summary>
internal interface IUserDataAccess
{
    /// <summary>
    /// Checks whether required user-related tables exist in the database schema.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A dictionary mapping table names to their existence status.</returns>
    Task<IDictionary<string, bool>> CheckRequiredTablesAsync(IDbConnection connection, CancellationToken ct = default);

    /// <summary>
    /// Finds a user by username including the password hash for credential verification.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="username">The username to look up.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The user model if found; otherwise null.</returns>
    Task<UserModel?> FindByUsernameWithHashAsync(IDbConnection connection, string username, CancellationToken ct = default);

    /// <summary>
    /// Finds a user by their unique identifier.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The user model if found; otherwise null.</returns>
    Task<UserModel?> FindByIdAsync(IDbConnection connection, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all roles and their permissions for a given user.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="userId">The user identifier to look up roles for.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The role models for the user.</returns>
    Task<IEnumerable<RoleModel>> GetRolesByUserIdAsync(IDbConnection connection, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Updates the last login timestamp for a user.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="userId">The user identifier to update.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> UpdateLastLoginAsync(IDbConnection connection, Guid userId, CancellationToken ct = default);
}
