using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using emc.camus.main.api.Configurations;
using System.Security.Cryptography;
using emc.camus.domain.Logging;
using emc.camus.application.Secrets;
using emc.camus.secretstorage.dapr;

namespace emc.camus.main.api.Handlers
{
    /// <summary>
    /// Provides extension methods for configuring dependency injection for application services.
    /// </summary>
    public static class DependencyInjectionHandler
    {
        private const string DefaultPemPath = "certificate.pem";
        /// <summary>
        /// Registers application services, JWT configuration, and secret providers in the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to add dependencies to.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddDependencyInjections(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure JWT --------------------------------------------------------
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            services.AddSingleton<RsaSecurityKey>(provider =>
            {
                var jwtSettings = provider.GetRequiredService<IOptions<JwtSettings>>().Value;
                var pemPath = Path.Combine(AppContext.BaseDirectory, jwtSettings.RsaPrivateKeyPem ?? DefaultPemPath);
                var pem = File.ReadAllText(pemPath);

                var rsa = RSA.Create();
                rsa.ImportFromPem(pem.ToCharArray());
                return new RsaSecurityKey(rsa);
            });

            services.AddSingleton<SigningCredentials>(provider =>
            {
                var rsaKey = provider.GetRequiredService<RsaSecurityKey>();
                return new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
            });

            // Configure App services -------------------------------------------------
            services.AddSingleton<IActivitySourceWrapper, ActivitySourceWrapper>(); 


            // Secret Provider Configuration ----------------------------------------
            services.AddSingleton<ISecretProvider>(provider => 
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(DaprSecretProvider));
                return new DaprSecretProvider(httpClient);
            });
            return services;
        }
    }
}