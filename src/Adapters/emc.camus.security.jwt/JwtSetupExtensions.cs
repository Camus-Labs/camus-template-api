using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using emc.camus.security.jwt.Configurations;
using emc.camus.security.jwt.Services;
using emc.camus.security.jwt.Handlers;
using emc.camus.application.Secrets;
using emc.camus.application.Auth;
using System.Diagnostics.CodeAnalysis;

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
            .AddJwtBearerWithDefaults(builder.Services);

            return builder;
        }
    }
}
