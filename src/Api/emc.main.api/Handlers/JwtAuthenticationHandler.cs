using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using emc.camus.main.api.Configurations;

namespace emc.camus.main.api.Handlers
{
    /// <summary>
    /// Provides extension methods for configuring JWT authentication.
    /// </summary>
    public static class JwtAuthenticationHandler
    {
        /// <summary>
        /// Adds and configures JWT Bearer authentication with validation parameters and event handlers.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="services">The service collection.</param>
        /// <returns>The updated authentication builder.</returns>
        public static AuthenticationBuilder AddCamusJwtAuthentication(this AuthenticationBuilder builder)
        {
            builder.AddJwtBearer();
            
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>, RsaSecurityKey>((options, jwtSettingsOptions, rsaKey) =>
                {
                    var jwtSettings = jwtSettingsOptions.Value;
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

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            var message = $"Authentication failed: {context.Exception.Message}";
                            logger.LogWarning(message);
                            throw new UnauthorizedAccessException(message, context.Exception);
                        },
                        OnChallenge = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            var message = "Authentication is required to access this resource";
                            logger.LogWarning(message);
                            throw new UnauthorizedAccessException(message);
                        },
                        OnForbidden = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            var message = "You do not have permission to access this resource";
                            logger.LogWarning(message);
                            throw new InvalidOperationException(message);
                        },
                        OnTokenValidated = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogInformation("Token validated for user: {User}", context.Principal?.Identity?.Name);
                            return Task.CompletedTask;
                        }
                    };
                });
            
            return builder;
        }
    }
}
