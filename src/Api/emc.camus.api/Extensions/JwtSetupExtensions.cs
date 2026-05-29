using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using emc.camus.api.Configurations;
using emc.camus.api.Exceptions;
using emc.camus.api.Utilities;
using emc.camus.application.Auth;
using emc.camus.application.Secrets;

namespace emc.camus.api.Extensions;

/// <summary>
/// Provides extension methods for configuring JWT authentication services.
/// </summary>
/// <remarks>
/// This class will replace <c>emc.camus.security.jwt.JwtSetupExtensions</c> once the adapter is removed.
/// During TDD scaffolding the method is non-extension to avoid ambiguity with the still-present adapter.
/// </remarks>
[ExcludeFromCodeCoverage]
public static class JwtSetupExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication services.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for fluent configuration.</returns>
    public static WebApplicationBuilder AddJwtAuthenticationInternal(WebApplicationBuilder builder)
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

            var pem = secretProvider.GetSecret(jwtSettings.RsaPrivateKeySecretName);

            try
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(pem.ToCharArray());
                return rsa;
            }
            catch (Exception ex)
            {
                throw new JwtKeyLoadException("Failed to load RSA signing key from PEM.", ex);
            }
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

        // Register TimeProvider (TryAdd avoids duplicate if another adapter already registered it)
        builder.Services.TryAddSingleton(TimeProvider.System);

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
                options.MapInboundClaims = true;

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
                    OnTokenValidated = context =>
                    {
                        var revocationCache = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationCache>();
                        var jtiClaim = context.SecurityToken?.Id;
                        if (jtiClaim != null && Guid.TryParse(jtiClaim, out var jti) && revocationCache.IsRevoked(jti))
                        {
                            context.Fail(new SecurityTokenException("Token has been revoked."));
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();

                        var originalException = context.AuthenticateFailure;

                        if (originalException != null)
                        {
                            string message = originalException switch
                            {
                                SecurityTokenExpiredException => "The provided credentials are invalid. JWT token has expired.",
                                SecurityTokenInvalidSignatureException => "The provided credentials are invalid. JWT token has invalid signature.",
                                SecurityTokenInvalidIssuerException => "The provided credentials are invalid. JWT token has invalid issuer.",
                                SecurityTokenInvalidAudienceException => "The provided credentials are invalid. JWT token has invalid audience.",
                                SecurityTokenException ex when ex.Message.Contains("revoked", StringComparison.OrdinalIgnoreCase)
                                    => "The provided credentials are invalid. JWT token has been revoked.",
                                _ => "The provided JWT token is malformed or invalid."
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
                        var userName = context.HttpContext.User.Identity?.Name ?? "Unknown";
                        throw new InvalidOperationException($"You do not have permission to access this resource. User: {userName}");
                    }
                };
            });

        return builder;
    }
}
