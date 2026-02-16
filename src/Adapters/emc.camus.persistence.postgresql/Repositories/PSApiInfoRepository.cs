using System.Data;
using Dapper;
using emc.camus.application.ApiInfo;
using emc.camus.application.Common;
using emc.camus.domain.Auth;
using emc.camus.persistence.postgresql.Mapping;
using emc.camus.persistence.postgresql.Models;
using Microsoft.Extensions.Logging;

namespace emc.camus.persistence.postgresql.Repositories;

/// <summary>
/// PostgreSQL implementation of API info repository using Dapper.
/// </summary>
public class PSApiInfoRepository : IApiInfoRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<PSApiInfoRepository> _logger;
    private bool _initialized = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="PSApiInfoRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for creating database connections.</param>
    /// <param name="logger">Logger for repository events.</param>
    public PSApiInfoRepository(
        IConnectionFactory connectionFactory,
        ILogger<PSApiInfoRepository> logger)
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
            _logger.LogWarning("PSApiInfoRepository already initialized. Skipping.");
            return;
        }

        try
        {
            // Test connection and verify table exists
            using var connection = _connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            
            const string checkTableSql = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'api_info'
                )";
            
            var tableExists = connection.ExecuteScalar<bool>(checkTableSql);
            
            if (!tableExists)
            {
                throw new InvalidOperationException(
                    "Required table 'api_info' does not exist in the database. " +
                    "Please run database migrations to create the schema.");
            }

            _initialized = true;
            _logger.LogInformation("PSApiInfoRepository initialized successfully");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to initialize PSApiInfoRepository");
            throw new InvalidOperationException(
                "Failed to initialize API info repository. Ensure the database is accessible.", ex);
        }
    }

    /// <summary>
    /// Gets API information by version from the database.
    /// </summary>
    /// <param name="version">The API version to retrieve.</param>
    /// <returns>An ApiInfo object if found.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when version is null or empty.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified version is not found.
    /// </exception>
    public async Task<ApiInfo> GetByVersionAsync(string version)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Version cannot be null or empty.", nameof(version));
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();

        const string sql = @"
            SELECT 
                name,
                version,
                status,
                features
            FROM api_info
            WHERE version = @Version";

        var result = await connection.QuerySingleOrDefaultAsync<ApiInfoModel>(
            sql, 
            new { Version = version });

        if (result == null)
        {
            _logger.LogWarning("API info not found for version {Version}", version);
            throw new KeyNotFoundException($"API info not found for version '{version}'.");
        }

        return result.ToEntity();
    }

    /// <summary>
    /// Gets all available API versions from the database.
    /// </summary>
    /// <returns>A list of all ApiInfo objects.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the repository has not been initialized.
    /// </exception>
    public async Task<List<ApiInfo>> GetAllAsync()
    {
        EnsureInitialized();

        using var connection = await _connectionFactory.CreateConnectionAsync();

        const string sql = @"
            SELECT 
                name,
                version,
                status,
                features
            FROM api_info
            ORDER BY version";

        var results = await connection.QueryAsync<ApiInfoModel>(sql);

        return results.Select(r => r.ToEntity()).ToList();
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call Initialize() first.");
        }
    }
}
