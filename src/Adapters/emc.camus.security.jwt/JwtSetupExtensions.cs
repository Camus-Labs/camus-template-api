using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using emc.camus.security.jwt.Configurations;
using emc.camus.security.jwt.Services;
using emc.camus.security.jwt.Handlers;
using emc.camus.application.Secrets;
using emc.camus.application.Auth;

namespace emc.camus.security.jwt
{
    /// <summary>
    /// Provides extension methods for configuring JWT authentication services.
    /// </summary>
    public static class JwtSetupExtensions
    {
        /// <summary>
        /// Adds JWT Bearer authentication services.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for fluent configuration.</returns>
        /// <remarks>
        /// This method requires an <see cref="ISecretProvider"/> to be registered in the service collection
        /// before calling this method. The secret provider must provide:
        /// - "RsaPrivateKeyPem": RSA private key in PEM format for JWT signing
        /// </remarks>
        public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
        {
            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("CamusSecuritySetup");
            
            logger.LogInformation("Configuring Camus authentication services");

            // Load, validate, and register JWT Settings
            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
            jwtSettings.Validate();
            builder.Services.AddSingleton(jwtSettings);

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
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearerWithDefaults(builder.Services, builder.Configuration);

            logger.LogInformation("JWT authentication configured with scheme: {Scheme}", 
                JwtBearerDefaults.AuthenticationScheme);

            return builder;
        }
    }
}
