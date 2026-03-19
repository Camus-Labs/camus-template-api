using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using emc.camus.application.Auth;
using emc.camus.application.Secrets;
using emc.camus.security.jwt.Configurations;
using emc.camus.security.jwt.Services;

namespace emc.camus.security.jwt
{
    /// <summary>
    /// Provides extension methods for configuring JWT authentication services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class JwtSetupExtensions
    {
        /// <summary>
        /// Adds JWT Bearer authentication services.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for fluent configuration.</returns>
        /// <remarks>
        /// This method requires an <see cref="ISecretProvider"/> to be registered in the service collection
        /// before calling this method. The secret provider must provide the RSA private key in PEM format
        /// using the secret name from <see cref="JwtSettings.RsaPrivateKeySecretName"/>.
        /// </remarks>
        public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
        {
            // Load, validate, and register JWT Settings
            var settings = builder.Configuration.GetSection(JwtSettings.ConfigurationSectionName).Get<JwtSettings>() ?? new JwtSettings();
            settings.Validate();
            builder.Services.AddSingleton(settings);

            // Register RSA key (DI container owns lifecycle and disposes at shutdown)
            builder.Services.AddSingleton<RSA>(provider =>
            {
                var secretProvider = provider.GetRequiredService<ISecretProvider>();
                var jwtSettings = provider.GetRequiredService<JwtSettings>();
                var pem = secretProvider.GetSecret(jwtSettings.RsaPrivateKeySecretName)
                    ?? throw new InvalidOperationException($"RSA private key '{jwtSettings.RsaPrivateKeySecretName}' not found in secrets");

                var rsa = RSA.Create();
                rsa.ImportFromPem(pem.ToCharArray());
                return rsa;
            });

            // Register RSA Security Key (wraps the DI-managed RSA instance)
            builder.Services.AddSingleton<RsaSecurityKey>(provider =>
            {
                var rsa = provider.GetRequiredService<RSA>();
                return new RsaSecurityKey(rsa);
            });

            // Register Signing Credentials
            builder.Services.AddSingleton<SigningCredentials>(provider =>
            {
                var rsaKey = provider.GetRequiredService<RsaSecurityKey>();
                return new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
            });

            // Register JWT Token Generator (implements ITokenGenerator for Application layer)
            builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

            // Configure Authentication schemes
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

            // Configure JWT Bearer Options with dependency injection
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
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
