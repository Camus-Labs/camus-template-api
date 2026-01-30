using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using emc.camus.main.api.Configurations;
using System.Security.Cryptography;
using emc.camus.domain.Logging;
using emc.camus.application.Secrets;
using emc.camus.secretstorage.dapr;
using emc.camus.secretstorage.dapr.Configurations;

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
            services.Configure<DaprSecretProviderSettings>(configuration.GetSection("DaprSecretProvider"));
            
            // HttpClient for DaprSecretProvider - configuration happens in the provider's constructor
            services.AddHttpClient<DaprSecretProvider>();

            services.AddSingleton<ISecretProvider>(provider => 
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(DaprSecretProvider));
                var logger = provider.GetRequiredService<ILogger<DaprSecretProvider>>();
                var settings = provider.GetRequiredService<IOptions<DaprSecretProviderSettings>>();
                return new DaprSecretProvider(httpClient, logger, settings);
            });
            return services;
        }

        /// <summary>
        /// Configures application-level dependency injections and initializes services at startup.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        public static async Task AppMappingsInjectionsAsync(this WebApplication app)
        {
            // Load secrets at application startup - this ensures they're loaded once and cached in the singleton
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var secretProvider = scope.ServiceProvider.GetRequiredService<ISecretProvider>();

                try
                {
                    // Get secret names from configuration or use default list
                    var secrets = app.Configuration.GetSection("SecretNames").Get<List<string>>() ?? new List<string> { };

                    logger.LogInformation("Loading {Count} secrets from Dapr secret store...", secrets.Count);

                    // Load secrets - they will be cached in the singleton instance
                    await secretProvider.LoadSecretsAsync(secrets);

                    var loadedCount  = secretProvider.GetLoadedSecretsCount();
                    logger.LogInformation("Successfully loaded {Count} our of {RequestedCount} secrets.", loadedCount, secrets.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load secrets from Dapr secret store.");
                    throw; // Rethrow to prevent app from starting without necessary secrets
                }
            }
        }
    }
}