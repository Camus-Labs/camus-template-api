using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.documentation.swagger.Configurations;
using emc.camus.documentation.swagger.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace emc.camus.documentation.swagger
{
    /// <summary>
    /// Provides extension methods for configuring Swagger/OpenAPI documentation.
    /// </summary>
    [ExcludeFromCodeCoverage]
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
            // Load, validate, and register Swagger Settings
            var settings = builder.Configuration.GetSection(SwaggerSettings.ConfigurationSectionName).Get<SwaggerSettings>() ?? new SwaggerSettings();
            settings.Validate();
            builder.Services.AddSingleton(settings);

            if (!settings.Enabled)
            {
                return builder;
            }

            // Use provided assembly or fall back to calling assembly
            var targetAssembly = apiAssembly ?? Assembly.GetCallingAssembly();

            builder.Services.AddSwaggerGen(options =>
            {
                ConfigureApiVersions(options, settings);
                ConfigureSecurityDefinitions(options, settings);
                ConfigureSecurityRequirements(options, settings);
                ConfigureXmlComments(options, settings, targetAssembly);
                options.EnableAnnotations();
                ConfigureFilters(options, settings);
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
            var settings = app.Services.GetRequiredService<SwaggerSettings>();

            if (!settings.Enabled || !app.Environment.IsDevelopment())
            {
                return app;
            }

            app.UseSwagger();
            ConfigureSwaggerUI(app, settings);
            ConfigureRootRedirect(app);

            return app;
        }

        /// <summary>
        /// Configures API version documentation.
        /// </summary>
        private static void ConfigureApiVersions(SwaggerGenOptions options, SwaggerSettings settings)
        {
            foreach (var versionInfo in settings.Versions)
            {
                options.SwaggerDoc(versionInfo.Version, new OpenApiInfo
                {
                    Title = versionInfo.Title,
                    Version = versionInfo.Version,
                    Description = versionInfo.Description
                });
            }
        }

        /// <summary>
        /// Configures security definitions based on configured schemes.
        /// </summary>
        private static void ConfigureSecurityDefinitions(SwaggerGenOptions options, SwaggerSettings settings)
        {
            foreach (var scheme in settings.SecuritySchemes)
            {
                var securityScheme = CreateSecurityScheme(scheme);
                if (securityScheme != null)
                {
                    var schemeKey = GetSecuritySchemeKey(scheme);
                    if (schemeKey != null)
                    {
                        options.AddSecurityDefinition(schemeKey, securityScheme);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an OpenAPI security scheme based on the scheme type.
        /// </summary>
        private static OpenApiSecurityScheme? CreateSecurityScheme(string scheme)
        {
            if (scheme.Equals(AuthenticationSchemes.JwtBearer, StringComparison.OrdinalIgnoreCase))
            {
                return new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
                    Name = HeaderNames.Authorization,
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = AuthenticationSchemes.JwtBearer.ToLowerInvariant(),
                    BearerFormat = "JWT"
                };
            }

            if (scheme.Equals(AuthenticationSchemes.ApiKey, StringComparison.OrdinalIgnoreCase))
            {
                return new OpenApiSecurityScheme
                {
                    Description = $"API Key needed to access the endpoints. Example: '{Headers.ApiKey}: {{key}}'",
                    Name = Headers.ApiKey,
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                };
            }

            return null;
        }

        /// <summary>
        /// Gets the security scheme key for the given scheme type.
        /// </summary>
        private static string? GetSecuritySchemeKey(string scheme)
        {
            if (scheme.Equals(AuthenticationSchemes.JwtBearer, StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticationSchemes.JwtBearer;
            }

            if (scheme.Equals(AuthenticationSchemes.ApiKey, StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticationSchemes.ApiKey;
            }

            return null;
        }

        /// <summary>
        /// Configures security requirements for all operations.
        /// </summary>
        private static void ConfigureSecurityRequirements(SwaggerGenOptions options, SwaggerSettings settings)
        {
            if (settings.SecuritySchemes.Count == 0)
            {
                return;
            }

            var securityRequirements = new OpenApiSecurityRequirement();

            foreach (var schemeKey in settings.SecuritySchemes
                .Select(GetSecuritySchemeKey)
                .Where(key => key != null))
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

            options.AddSecurityRequirement(securityRequirements);
        }

        /// <summary>
        /// Configures XML comment documentation.
        /// </summary>
        private static void ConfigureXmlComments(SwaggerGenOptions options, SwaggerSettings settings, Assembly targetAssembly)
        {
            if (!settings.IncludeXmlComments)
            {
                return;
            }

            var xmlFile = $"{targetAssembly.GetName().Name}.xml";
            var xmlPath = Path.Join(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        }

        /// <summary>
        /// Configures Swagger filters.
        /// </summary>
        private static void ConfigureFilters(SwaggerGenOptions options, SwaggerSettings settings)
        {
            options.OperationFilter<DefaultApiResponsesOperationFilter>();

            if (settings.EnableExampleFilters)
            {
                options.ExampleFilters();
            }
        }

        /// <summary>
        /// Configures Swagger UI endpoints.
        /// </summary>
        private static void ConfigureSwaggerUI(WebApplication app, SwaggerSettings settings)
        {
            app.UseSwaggerUI(options =>
            {
                foreach (var versionInfo in settings.Versions)
                {
                    options.SwaggerEndpoint($"/swagger/{versionInfo.Version}/swagger.json", versionInfo.Title);
                }
            });
        }

        /// <summary>
        /// Configures root path redirect to Swagger UI.
        /// </summary>
        private static void ConfigureRootRedirect(WebApplication app)
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
    }
}
