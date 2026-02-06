using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using emc.camus.security.components.Configurations;

namespace emc.camus.security.components.Handlers
{
    /// <summary>
    /// Provides extension methods for configuring JWT Bearer authentication.
    /// </summary>
    public static class JwtAuthenticationHandler
    {
        /// <summary>
        /// Adds JWT Bearer authentication with default configuration including event handlers for logging.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="services">The service collection for dependency resolution.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated authentication builder for fluent configuration.</returns>
        public static AuthenticationBuilder AddJwtBearerWithDefaults(
            this AuthenticationBuilder builder,
            IServiceCollection services,
            IConfiguration configuration)
        {
            builder.AddJwtBearer(options =>
            {
                // Initial configuration - will be overridden by AddOptions below
            });

            // Configure JWT Bearer Options with dependency injection
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>, RsaSecurityKey, ILoggerFactory>((options, jwtSettingsOptions, rsaKey, loggerFactory) =>
                {
                    var jwtSettings = jwtSettingsOptions.Value;
                    var logger = loggerFactory.CreateLogger("JwtAuthenticationHandler");
                    
                    // Token Validation Parameters
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = rsaKey,
                        ClockSkew = TimeSpan.Zero
                    };

                    // JWT Bearer Events for logging and error handling
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var message = $"JWT Authentication failed: {context.Exception.Message}";
                            logger.LogWarning(message);
                            throw new UnauthorizedAccessException(message, context.Exception);
                        },
                        OnChallenge = context =>
                        {
                            var message = "JWT Authentication required to access this resource";
                            logger.LogWarning(message);
                            throw new UnauthorizedAccessException(message);
                        },
                        OnForbidden = context =>
                        {
                            var message = $"JWT Authorization forbidden for user: {context.Principal?.Identity?.Name ?? "Unknown"}";
                            logger.LogWarning(message);
                            throw new InvalidOperationException(message);
                        },
                        OnTokenValidated = context =>
                        {
                            logger.LogInformation("JWT Token validated for user: {User}", 
                                context.Principal?.Identity?.Name ?? "Unknown");
                            return Task.CompletedTask;
                        }
                    };
                });

            return builder;
        }
    }
}
