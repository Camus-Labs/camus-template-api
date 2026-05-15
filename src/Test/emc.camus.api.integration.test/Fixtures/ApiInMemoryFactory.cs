using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using emc.camus.api.integration.test.Helpers;

namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Factory variant for integration testing with in-memory persistence.
/// Uses the default InMemory provider from appsettings.json.
/// DBUp migrations are explicitly disabled.
/// Registers test-only controllers from the integration test assembly for cross-cutting concern testing.
/// </summary>
public class ApiInMemoryFactory : ApiFactoryBase
{
    protected override void ConfigureVariantHostSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("DataPersistenceSettings:Provider", "InMemory");
        builder.UseSetting("DBUpSettings:Enabled", "false");

        builder.ConfigureServices(services =>
        {
            services.AddControllers()
                .AddApplicationPart(typeof(IdempotencyTestController).Assembly);
        });
    }
}
