using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using emc.camus.security.components.Configurations;
using emc.camus.security.components.Handlers;
using emc.camus.security.components.Services;
using emc.camus.application.Secrets;
using emc.camus.application.Auth;

namespace emc.camus.security.components
{
    /// <summary>
    /// Provides extension methods for configuring Camus security services including authentication and authorization.
    /// </summary>
    public static class SecuritySetupExtensions
    {
        /// <summary>
        /// Adds Camus authentication services including JWT Bearer and API Key authentication.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for fluent configuration.</returns>
        /// <remarks>
        /// This method requires an <see cref="ISecretProvider"/> to be registered in the service collection
        /// before calling this method. The secret provider must provide:
        /// - "RsaPrivateKeyPem": RSA private key in PEM format for JWT signing
        /// - "XApiKey": API key for X-API-Key header authentication
        /// </remarks>
        public static WebApplicationBuilder AddCamusAuthentication(this WebApplicationBuilder builder)
        {
            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("CamusSecuritySetup");
            
            logger.LogInformation("Configuring Camus authentication services");

            // Configure JWT Settings
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

            // Register RSA Security Key (requires ISecretProvider)
            builder.Services.AddSingleton<RsaSecurityKey>(provider =>
            {
                var secretProvider = provider.GetRequiredService<ISecretProvider>();
                var pem = secretProvider.GetSecret("RsaPrivateKeyPem")
                    ?? throw new InvalidOperationException("RSA private key 'RsaPrivateKeyPem' not found in secrets");

                var rsa = RSA.Create();
                rsa.ImportFromPem(pem.ToCharArray());
                logger.LogInformation("RSA security key loaded successfully");
                return new RsaSecurityKey(rsa);
            });

            // Register Signing Credentials
            builder.Services.AddSingleton<SigningCredentials>(provider =>
            {
                var rsaKey = provider.GetRequiredService<RsaSecurityKey>();
                return new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
            });

            // Register JWT Token Generator
            builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

            // Configure Authentication schemes
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CamusAuthenticationSchemes.JwtBearer;
                options.DefaultChallengeScheme = CamusAuthenticationSchemes.JwtBearer;
            })
            .AddJwtBearerWithDefaults(builder.Services, builder.Configuration)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                CamusAuthenticationSchemes.ApiKey,
                null);

            logger.LogInformation("Camus authentication configured with schemes: {Schemes}", 
                $"{CamusAuthenticationSchemes.JwtBearer}, {CamusAuthenticationSchemes.ApiKey}");

            return builder;
        }

        /// <summary>
        /// Adds Camus authorization services with default policies.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for fluent configuration.</returns>
        /// <remarks>
        /// Currently configures basic authorization. Can be extended with custom policies,
        /// role-based authorization, claims-based authorization, or resource-based authorization.
        /// Access to Configuration is available for future policy configuration needs.
        /// </remarks>
        public static WebApplicationBuilder AddCamusAuthorization(this WebApplicationBuilder builder)
        {
            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("CamusSecuritySetup");
            
            logger.LogInformation("Configuring Camus authorization services");

            builder.Services.AddAuthorization(options =>
            {
                // Future: Add custom authorization policies here using builder.Configuration if needed
                // Example:
                // options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                // options.AddPolicy("RequireApiClientClaim", policy => policy.RequireClaim(ClaimTypes.Role, "ApiClient"));
            });

            logger.LogInformation("Camus authorization configured");

            return builder;
        }
    }
}
