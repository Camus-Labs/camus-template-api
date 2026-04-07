using Microsoft.AspNetCore.Hosting;

namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Factory variant for integration testing with in-memory persistence.
/// Uses the default InMemory provider from appsettings.json.
/// DBUp migrations are explicitly disabled.
/// </summary>
public class CamusApiIMFactory : CamusApiFactoryBase
{
    protected override void ConfigureVariantHostSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("DBUpSettings:Enabled", "false");
    }
}
