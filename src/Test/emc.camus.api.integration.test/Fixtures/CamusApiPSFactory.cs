using Microsoft.AspNetCore.Hosting;
using Testcontainers.PostgreSql;

namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Factory variant backed by a real PostgreSQL Testcontainer.
/// Configures the PostgreSQL persistence provider with DBUp migrations enabled,
/// so the full persistence stack (migrations → repositories → database) is exercised.
/// All settings are supplied via <see cref="CamusApiFactoryBase.ConfigureVariantHostSettings"/>
/// to ensure they are available during <c>Program.cs</c> service registration.
/// </summary>
public class CamusApiPSFactory : CamusApiFactoryBase
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    /// <summary>
    /// Returns the Testcontainer connection string for direct database assertions in tests.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    public override async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    protected override async Task CleanupAsync()
    {
        await _container.DisposeAsync();
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
