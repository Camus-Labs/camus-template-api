using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Configurations;
using emc.camus.api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace emc.camus.api.Extensions;

/// <summary>
/// Provides extension methods for configuring idempotency key validation services.
/// </summary>
[ExcludeFromCodeCoverage]
public static class IdempotencySetupExtensions
{
    /// <summary>
    /// Registers idempotency validation services and configuration in the DI container.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for method chaining.</returns>
    public static WebApplicationBuilder AddIdempotency(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration
            .GetSection(IdempotencySettings.ConfigurationSectionName)
            .Get<IdempotencySettings>() ?? new IdempotencySettings();

        settings.Validate();

        builder.Services.AddSingleton(settings);
        builder.Services.AddScoped<IdempotencyKeyValidationFilter>();
        builder.Services.Configure<MvcOptions>(options =>
        {
            options.Filters.AddService<IdempotencyKeyValidationFilter>();
        });

        return builder;
    }
}
