using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using emc.camus.security.jwt.Configurations;
using emc.camus.application.Auth;
using System.IdentityModel.Tokens.Jwt;

namespace emc.camus.security.jwt.Handlers
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
        /// <returns>The updated authentication builder for fluent configuration.</returns>
        public static AuthenticationBuilder AddJwtBearerWithDefaults(
            this AuthenticationBuilder builder,
            IServiceCollection services)
        {
            builder.AddJwtBearer(options =>
            {
                // Initial configuration - will be overridden by AddOptions below
            });

            // Configure JWT Bearer Options with dependency injection
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<JwtSettings, RsaSecurityKey>((options, jwtSettings, rsaKey) =>
                {
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
                        OnTokenValidated = context =>
                        {
                            // Check if the token has been revoked via in-memory cache
                            var revocationCache = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationCache>();
                            var jtiClaim = (context.SecurityToken as JwtSecurityToken)?.Id;
                            if (jtiClaim != null && Guid.TryParse(jtiClaim, out var jti) && revocationCache.IsRevoked(jti))
                            {
                                context.Fail(new SecurityTokenException("Token has been revoked."));
                            }

                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            // Skip default challenge response - we'll throw to use our error handler
                            context.HandleResponse();

                            // AuthenticateFailure is populated by the framework from AuthenticateResult.Failure
                            // This covers both framework validation errors and custom context.Fail() calls
                            var originalException = context.AuthenticateFailure;
                            
                            // Throw exception - middleware will auto-detect error code from message pattern
                            if (originalException != null)
                            {
                                // Create specific message based on exception type for middleware pattern matching
                                string message = originalException switch
                                {
                                    SecurityTokenExpiredException => "The provided credentials are invalid. JWT token has expired.",
                                    SecurityTokenInvalidSignatureException => "The provided credentials are invalid. JWT token has invalid signature.",
                                    SecurityTokenInvalidIssuerException => "The provided credentials are invalid. JWT token has invalid issuer.",
                                    SecurityTokenInvalidAudienceException => "The provided credentials are invalid. JWT token has invalid audience.",
                                    SecurityTokenException ex when ex.Message.Contains("revoked", StringComparison.OrdinalIgnoreCase) 
                                        => "The provided credentials are invalid. JWT token has been revoked.",
                                    _ => "The provided credentials are invalid. Invalid JWT token."
                                };
                                throw new UnauthorizedAccessException(message, originalException);
                            }
                            else
                            {
                                throw new UnauthorizedAccessException("Authentication is required to access this resource. No valid JWT token was provided.");
                            }
                        },
                        OnForbidden = context =>
                        {
                            var userName = context.Principal?.Identity?.Name ?? "Unknown";
                            throw new InvalidOperationException($"You do not have permission to access this resource. User: {userName}");
                        }
                    };
                });

            return builder;
        }
    }
}
