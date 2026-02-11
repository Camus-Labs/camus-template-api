using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using emc.camus.api.Configurations;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring application-level services and policies.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ApplicationServicesExtensions
    {
        /// <summary>
        /// Registers all application-level services including API versioning, CORS, authorization,
        /// controllers, and business logic dependencies.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
        {
            // Configure API versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version")
                );
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // Configure CORS policy
            var settings = builder.Configuration.GetSection(CorsSettings.ConfigurationSectionName).Get<CorsSettings>() ?? new CorsSettings();
            settings.Validate();
            builder.Services.AddSingleton(settings);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(settings.PolicyName, policy =>
                {
                    policy.WithOrigins(settings.AllowedOrigins)
                          .WithMethods(settings.AllowedMethods)
                          .WithHeaders(settings.AllowedHeaders)
                          .WithExposedHeaders(settings.ExposedHeaders);

                    if (settings.AllowCredentials)
                    {
                        policy.AllowCredentials();
                    }

                    policy.SetPreflightMaxAge(TimeSpan.FromMinutes(settings.PreflightMaxAgeMinutes));
                });
            });

            // Configure Authorization (application-specific policies)
            builder.Services.AddAuthorization(options =>
            {
                // Add custom authorization policies here as needed
                // Example: options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
            });

            // Add controllers
            builder.Services.AddControllers();

            // Register application-specific business services
            // Example: builder.Services.AddScoped<IUserRepository, UserRepository>();
            // Example: builder.Services.AddScoped<IOrderService, OrderService>();

            return builder;
        }

        /// <summary>
        /// Configures application-level middleware and endpoint routing in the request pipeline.
        /// Applies CORS policy, rate limiting, and maps controller endpoints.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The web application instance for method chaining.</returns>
        public static WebApplication UseApplicationServices(this WebApplication app)
        {
            // Load CORS settings from DI and Apply so all responses include the proper CORS headers
            var corsSettings = app.Services.GetRequiredService<CorsSettings>();
            app.UseCors(corsSettings.PolicyName);

            // Map controller endpoints
            app.MapControllers();
            
            return app;
        }
    }
}