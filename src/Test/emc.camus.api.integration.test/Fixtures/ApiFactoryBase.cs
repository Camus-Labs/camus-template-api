using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MartinCostello.Logging.XUnit;
using emc.camus.application.Secrets;
using emc.camus.api.integration.test.Helpers;

namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Base <see cref="WebApplicationFactory{TEntryPoint}"/> for integration testing.
/// Configures shared concerns: Testing environment, stub secrets, default HTTP headers,
/// and test output logging via <see cref="ITestOutputHelperAccessor"/>.
/// All configuration is supplied via <see cref="IWebHostBuilder.UseSetting"/> to ensure values
/// are available during <c>Program.cs</c> service registration.
/// Derived factories supply variant-specific settings via <see cref="ConfigureVariantHostSettings"/>.
/// </summary>
public abstract class ApiFactoryBase : WebApplicationFactory<Program>, IAsyncLifetime, ITestOutputHelperAccessor
{
    public Xunit.Abstractions.ITestOutputHelper? OutputHelper { get; set; }

    public virtual async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await CleanupAsync();
    }

    /// <summary>
    /// Override to release variant-specific resources (e.g., Testcontainers) after the factory is disposed.
    /// </summary>
    protected virtual Task CleanupAsync() => Task.CompletedTask;

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Shared settings for all test variants
        builder.UseSetting("SwaggerSettings:Enabled", "false");
        builder.UseSetting("DaprSecretProviderSettings:BaseHost", "localhost");
        builder.UseSetting("DaprSecretProviderSettings:HttpPort", "3500");
        builder.UseSetting("DaprSecretProviderSettings:SecretStoreName", "test-store");
        builder.UseSetting("AllowedHosts", "*");
        builder.UseSetting("RateLimitSettings:Policies:default:PermitLimit", "10000");
        builder.UseSetting("RateLimitSettings:Policies:default:WindowSeconds", "60");
        builder.UseSetting("RateLimitSettings:Policies:strict:PermitLimit", "10000");
        builder.UseSetting("RateLimitSettings:Policies:strict:WindowSeconds", "60");
        builder.UseSetting("RateLimitSettings:Policies:relaxed:PermitLimit", "10000");
        builder.UseSetting("RateLimitSettings:Policies:relaxed:WindowSeconds", "60");

        // Variant-specific settings
        ConfigureVariantHostSettings(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISecretProvider>();
            services.AddSingleton<ISecretProvider>(new StubSecretProvider());
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddXUnit(this);
        });
    }

    /// <summary>
    /// Override to set variant-specific configuration values via <see cref="IWebHostBuilder.UseSetting"/>.
    /// Called after shared settings are applied, so variant values take precedence.
    /// </summary>
    protected virtual void ConfigureVariantHostSettings(IWebHostBuilder builder) { }
}
