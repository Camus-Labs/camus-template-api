using emc.camus.application.Auth;
using emc.camus.documentation.swagger.Configurations;
using emc.camus.documentation.swagger.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;

namespace emc.camus.documentation.swagger
{
    /// <summary>
    /// Provides extension methods for configuring Swagger/OpenAPI documentation.
    /// </summary>
    public static class SwaggerSetupExtensions
    {
        /// <summary>
        /// Adds Swagger/OpenAPI documentation with configured security schemes and versioning.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <param name="apiAssembly">Optional assembly for Swagger examples. If not provided, calling assembly is used.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddSwaggerDocumentation(
            this WebApplicationBuilder builder,
            Assembly? apiAssembly = null)
        {
            var settings = builder.Configuration.GetSection("SwaggerSettings").Get<SwaggerSettings>() ?? new SwaggerSettings();

            if (!settings.Enabled)
            {
                return builder;
            }

            // Use provided assembly or fall back to calling assembly
            var targetAssembly = apiAssembly ?? Assembly.GetCallingAssembly();

            builder.Services.AddSwaggerGen(options =>
            {
                // Configure API versions
                foreach (var versionInfo in settings.Versions)
                {
                    options.SwaggerDoc(versionInfo.Version, new OpenApiInfo
                    {
                        Title = versionInfo.Title,
                        Version = versionInfo.Version,
                        Description = versionInfo.Description
                    });
                }

                // Add security definitions based on configured schemes
                foreach (var scheme in settings.SecuritySchemes)
                {
                    switch (scheme.ToLowerInvariant())
                    {
                        case "bearer":
                        case "jwt":
                            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                            {
                                Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
                                Name = "Authorization",
                                In = ParameterLocation.Header,
                                Type = SecuritySchemeType.Http,
                                Scheme = "bearer",
                                BearerFormat = "JWT"
                            });
                            break;

                        case "apikey":
                            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                            {
                                Description = "API Key needed to access the endpoints. Example: 'X-Api-Key: {key}'",
                                Name = "X-Api-Key",
                                In = ParameterLocation.Header,
                                Type = SecuritySchemeType.ApiKey
                            });
                            break;
                    }
                }

                // Add security requirements for configured schemes
                if (settings.SecuritySchemes.Any())
                {
                    var securityRequirements = new OpenApiSecurityRequirement();

                    foreach (var scheme in settings.SecuritySchemes)
                    {
                        var schemeKey = scheme.ToLowerInvariant() switch
                        {
                            "bearer" or "jwt" => "Bearer",
                            "apikey" => "ApiKey",
                            _ => null
                        };

                        if (schemeKey != null)
                        {
                            securityRequirements.Add(
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = schemeKey
                                    }
                                },
                                Array.Empty<string>()
                            );
                        }
                    }

                    options.AddSecurityRequirement(securityRequirements);
                }

                // Include XML comments if enabled
                if (settings.IncludeXmlComments)
                {
                    var xmlFile = $"{targetAssembly.GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                    {
                        options.IncludeXmlComments(xmlPath);
                    }
                }

                // Enable annotations if configured
                if (settings.EnableAnnotations)
                {
                    options.EnableAnnotations();
                }

                // Add default API responses operation filter
                options.OperationFilter<DefaultApiResponsesOperationFilter>();

                // Enable example filters if configured
                if (settings.EnableExampleFilters)
                {
                    options.ExampleFilters();
                }
            });

            // Register Swagger examples from the target assembly if example filters are enabled
            if (settings.EnableExampleFilters)
            {
                builder.Services.AddSwaggerExamplesFromAssemblies(targetAssembly);
            }

            return builder;
        }

        /// <summary>
        /// Configures Swagger UI middleware for development environments.
        /// </summary>
        /// <param name="app">The web application.</param>
        /// <returns>The web application for method chaining.</returns>
        public static WebApplication UseSwaggerDocumentation(this WebApplication app)
        {
            var settings = app.Configuration.GetSection("SwaggerSettings").Get<SwaggerSettings>() ?? new SwaggerSettings();

            if (!settings.Enabled || !app.Environment.IsDevelopment())
            {
                return app;
            }

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var versionInfo in settings.Versions)
                {
                    options.SwaggerEndpoint($"/swagger/{versionInfo.Version}/swagger.json", versionInfo.Title);
                }
            });

            // Redirect root path to Swagger UI if configured
            if (settings.RedirectRootToSwagger)
            {
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path == "/" || context.Request.Path == string.Empty)
                    {
                        context.Response.Redirect("/swagger/index.html", permanent: false);
                        return;
                    }
                    await next();
                });
            }

            return app;
        }
    }
}
