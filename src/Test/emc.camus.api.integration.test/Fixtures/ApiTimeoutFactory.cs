using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using emc.camus.application.ApiInfo;
using emc.camus.api.integration.test.Helpers;

namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Factory variant for integration testing request timeout behavior.
/// Uses in-memory persistence with a slow <see cref="IApiInfoService"/> stub and a 1-second tight timeout
/// to force the request timeout policy to fire on endpoints using the tight policy.
/// </summary>
public class ApiTimeoutFactory : ApiFactoryBase
{
    protected override void ConfigureVariantHostSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("DataPersistenceSettings:Provider", "InMemory");
        builder.UseSetting("DBUpSettings:Enabled", "false");
        builder.UseSetting("RequestTimeoutSettings:TightTimeoutSeconds", "1");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IApiInfoService>();
            services.AddSingleton<SlowApiInfoService>();
            services.AddSingleton<IApiInfoService>(sp => sp.GetRequiredService<SlowApiInfoService>());
        });
    }
}
