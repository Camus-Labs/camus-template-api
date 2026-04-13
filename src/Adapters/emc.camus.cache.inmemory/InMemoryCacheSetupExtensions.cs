using System.Diagnostics.CodeAnalysis;
using emc.camus.application.Auth;
using emc.camus.cache.inmemory.Configurations;
using emc.camus.cache.inmemory.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace emc.camus.cache.inmemory;

/// <summary>
/// Provides extension methods for registering in-memory cache services.
/// </summary>
[ExcludeFromCodeCoverage]
public static class InMemoryCacheSetupExtensions
{
    /// <summary>
    /// Registers in-memory cache adapters including token revocation cache and background sync service.
    /// For multi-instance deployments, replace with the Redis cache adapter.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for method chaining.</returns>
    public static WebApplicationBuilder AddInMemoryCache(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration.GetSection(InMemoryCacheSettings.ConfigurationSectionName).Get<InMemoryCacheSettings>()
            ?? new InMemoryCacheSettings();
        settings.Validate();

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<ITokenRevocationCache, TokenRevocationCache>();

        if (settings.TokenRevocationCache.SyncEnabled)
        {
            builder.Services.AddHostedService<TokenRevocationSyncService>();
        }

        return builder;
    }
}
