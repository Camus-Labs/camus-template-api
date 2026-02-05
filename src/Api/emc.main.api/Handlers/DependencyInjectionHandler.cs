using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using emc.camus.main.api.Configurations;
using System.Security.Cryptography;
using emc.camus.domain.Logging;
using emc.camus.application.Secrets;
using emc.camus.secretstorage.dapr;
using emc.camus.secretstorage.dapr.Configurations;
using emc.camus.main.api.SwaggerExamples;
using Swashbuckle.AspNetCore.Filters;

namespace emc.camus.main.api.Handlers
{
    /// <summary>
    /// Provides extension methods for configuring dependency injection for application services.
    /// </summary>
    public static class DependencyInjectionHandler
    {
        /// <summary>
        /// Registers application services, JWT configuration, and secret providers in the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to add dependencies to.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddDependencyInjections(this IServiceCollection services, IConfiguration configuration)
        {
            // Swagger Examples Configuration ----------------------------------------
            // Registers all IExamplesProvider implementations from the assembly containing ApiInfoExample thats why only one is needed
            services.AddSwaggerExamplesFromAssemblyOf<ApiInfoExample>();

            // Secret Provider Configuration ----------------------------------------
            services.Configure<DaprSecretProviderSettings>(configuration.GetSection("DaprSecretProvider"));
            
            // Register DaprSecretProvider - secrets load in constructor
            services.AddHttpClient<DaprSecretProvider>();
            services.AddSingleton<ISecretProvider>(provider => provider.GetRequiredService<DaprSecretProvider>());

            // Configure JWT --------------------------------------------------------
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            services.AddSingleton<RsaSecurityKey>(provider =>
            {
                // Load RSA private key from Dapr secret
                var secretProvider = provider.GetRequiredService<ISecretProvider>();
                var pem = secretProvider.GetSecret("RsaPrivateKeyPem") 
                    ?? throw new InvalidOperationException("RSA private key 'RsaPrivateKeyPem' not found in secrets");

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

            return services;
        }

        /// <summary>
        /// Configures application-level dependency injections and initializes services at startup.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        public static Task AppMappingsInjectionsAsync(this WebApplication app)
        {
            // Force ISecretProvider singleton creation during startup
            // This loads all secrets and fails fast if there are any issues
            app.Services.GetRequiredService<ISecretProvider>();
            
            return Task.CompletedTask;
        }
    }
}