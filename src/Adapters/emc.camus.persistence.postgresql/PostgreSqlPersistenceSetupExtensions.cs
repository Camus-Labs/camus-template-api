using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using emc.camus.application.Common;
using emc.camus.application.ApiInfo;
using emc.camus.application.Auth;
using emc.camus.persistence.postgresql.Services;
using emc.camus.persistence.postgresql.DataAccess;
using emc.camus.persistence.postgresql.Repositories;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace emc.camus.persistence.postgresql
{
    /// <summary>
    /// Provides extension methods for configuring PostgreSQL persistence services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class PostgreSqlPersistenceSetupExtensions
    {
        private static readonly string[] ReadyTag = new[] { "ready" };
        /// <summary>
        /// Adds PostgreSQL persistence services including connection factory, unit of work, and all repositories.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        /// <remarks>
        /// Requires <see cref="emc.camus.application.Configurations.DatabaseSettings"/>
        /// to be registered as a singleton before calling this method.
        /// Requires IUserContext to be registered before calling this method (mandatory for audit trail functionality).
        /// Automatically sets session variables (app.current_username) for database triggers.
        /// Will throw exception during service resolution if IUserContext is not registered.
        /// </remarks>
        public static WebApplicationBuilder AddPostgreSqlPersistence(this WebApplicationBuilder builder)
        {
            // Enable Dapper snake_case → PascalCase column mapping (e.g., password_hash → PasswordHash)
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            // Register database connection factory (scoped: depends on scoped IUserContext for audit session variables)
            builder.Services.AddScoped<IConnectionFactory, ConnectionFactory>();

            // Register initialization state (singleton: shared across scoped repositories within one container)
            builder.Services.AddSingleton<InitializationState>();

            // Register unit of work (scoped: one per request, shared across repositories)
             // Registered as concrete type for internal repository access (GetConnectionAsync),
            // and forwarded to IUnitOfWork for application-layer consumers.
            builder.Services.AddScoped<UnitOfWork>();
            builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());

            // Register audit repository (shared across Auth and AppData)
            builder.Services.AddScoped<IActionAuditRepository, ActionAuditRepository>();

            // Register data access layers
            builder.Services.AddSingleton<IUserDataAccess, UserDataAccess>();
            builder.Services.AddSingleton<IApiInfoDataAccess, ApiInfoDataAccess>();
            builder.Services.AddSingleton<IGeneratedTokenDataAccess, GeneratedTokenDataAccess>();
            builder.Services.AddSingleton<IActionAuditDataAccess, ActionAuditDataAccess>();

            // Register repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IGeneratedTokenRepository, GeneratedTokenRepository>();
            builder.Services.AddScoped<IApiInfoRepository, ApiInfoRepository>();

            return builder;
        }

        /// <summary>
        /// Registers a PostgreSQL health check tagged with "ready" for readiness probes.
        /// Resolves <see cref="UnitOfWork"/> at runtime to verify database connectivity.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <returns>The health checks builder for method chaining.</returns>
        public static IHealthChecksBuilder AddPostgreSqlHealthCheck(this IHealthChecksBuilder builder)
        {
            builder.Add(new HealthCheckRegistration(
                "postgresql",
                sp =>
                {
                    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
                    return new HealthCheck(unitOfWork);
                },
                failureStatus: null,
                tags: ReadyTag));

            return builder;
        }
    }
}
