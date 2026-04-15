using emc.camus.api.integration.test.Helpers;
using Microsoft.AspNetCore.Hosting;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Factory variant backed by a real PostgreSQL Testcontainer.
/// Configures the PostgreSQL persistence provider with DBUp migrations enabled,
/// so the full persistence stack (migrations → repositories → database) is exercised.
/// All settings are supplied via <see cref="ApiFactoryBase.ConfigureVariantHostSettings"/>
/// to ensure they are available during <c>Program.cs</c> service registration.
/// </summary>
public class ApiPostgreSqlFactory : ApiFactoryBase
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private Respawner? _respawner;

    /// <summary>
    /// Returns the Testcontainer connection string for direct database assertions in tests.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    public override async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // Force host creation so Program.cs runs and DBUp migrations execute
        // before any test tries to reset the database via Respawn.
        _ = Server;
    }

    protected override async Task CleanupAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Resets the database to its post-migration state by deleting all data and re-seeding.
    /// The <see cref="Respawner"/> is created lazily on first call, after migrations have run.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["camus"],
        });

        await _respawner.ResetAsync(connection);
        await DatabaseSeeder.SeedAsync(connection);
    }

    protected override void ConfigureVariantHostSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("DataPersistenceSettings:Provider", "PostgreSQL");
        builder.UseSetting("DatabaseSettings:Host", _container.Hostname);
        builder.UseSetting("DatabaseSettings:Port", _container.GetMappedPublicPort(5432).ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("DatabaseSettings:Database", PostgreSqlBuilder.DefaultDatabase);
        builder.UseSetting("DatabaseSettings:UserSecretName", "DBUser");
        builder.UseSetting("DatabaseSettings:PasswordSecretName", "DBSecret");
        builder.UseSetting("DatabaseSettings:AdditionalParameters", "Timeout=30;Pooling=true;MinPoolSize=1;MaxPoolSize=5");
        builder.UseSetting("DBUpSettings:Enabled", "true");
        builder.UseSetting("DBUpSettings:AdminSecretName", "DBMigrationsUser");
        builder.UseSetting("DBUpSettings:PasswordSecretName", "DBMigrationsSecret");
    }
}
