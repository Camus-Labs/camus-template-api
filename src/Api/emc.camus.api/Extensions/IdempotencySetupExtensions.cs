using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Configurations;
using emc.camus.api.Filters;
using emc.camus.api.Metrics;
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
    /// <param name="serviceName">The service name for metrics instrumentation.</param>
    /// <returns>The web application builder for method chaining.</returns>
    public static WebApplicationBuilder AddIdempotency(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        var settings = builder.Configuration
            .GetSection(IdempotencySettings.ConfigurationSectionName)
            .Get<IdempotencySettings>() ?? new IdempotencySettings();

        settings.Validate();

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(new IdempotencyMetrics(serviceName));
        builder.Services.AddScoped<IdempotencyKeyValidationFilter>();
        builder.Services.AddScoped<IdempotencyResponseCachingFilter>();
        builder.Services.Configure<MvcOptions>(options =>
        {
            options.Filters.AddService<IdempotencyKeyValidationFilter>();
            options.Filters.AddService<IdempotencyResponseCachingFilter>();
        });

        return builder;
    }
}
