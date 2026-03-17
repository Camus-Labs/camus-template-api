using System.Diagnostics.CodeAnalysis;
using emc.camus.application.Auth;
using emc.camus.cache.inmemory.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace emc.camus.cache.inmemory;

/// <summary>
/// Provides extension methods for registering in-memory cache services.
/// </summary>
[ExcludeFromCodeCoverage]
public static class InMemoryCacheSetupExtensions
{
    /// <summary>
    /// Registers in-memory cache adapters including token revocation cache.
    /// For multi-instance deployments, replace with the Redis cache adapter.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for method chaining.</returns>
    public static WebApplicationBuilder AddInMemoryCache(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton<ITokenRevocationCache, IMTokenRevocationCache>();

        return builder;
    }
}
