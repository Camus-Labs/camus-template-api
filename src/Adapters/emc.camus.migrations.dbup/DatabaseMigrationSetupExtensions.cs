using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DbUp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using emc.camus.application.Configurations;
using emc.camus.application.Secrets;
using emc.camus.migrations.dbup.Configurations;

namespace emc.camus.migrations.dbup
{
    /// <summary>
    /// Provides extension methods for database migration using DbUp.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static partial class DatabaseMigrationSetupExtensions
    {
        private const string DefaultSchema = "public";
        private const string DefaultSchemaVersionsTable = "camus_schemaversions";

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Starting database migrations...")]
        private static partial void LogMigrationStarting(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Database migration credentials will be fetched from secret provider")]
        private static partial void LogCredentialsFetchFromSecrets(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Database is up to date. No migrations needed.")]
        private static partial void LogDatabaseUpToDate(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Database migrations completed successfully")]
        private static partial void LogMigrationCompleted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Executed migration: {ScriptName}")]
        private static partial void LogMigrationScriptExecuted(ILogger logger, string scriptName);

        /// <summary>
        /// Registers database migration configuration and validates settings.
        /// Skips registration when <see cref="DBUpSettings.Enabled"/> is <c>false</c>.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddDatabaseMigrations(this WebApplicationBuilder builder)
        {
            // Load DBUpSettings — skip if missing or disabled
            var migrationsSettings = builder.Configuration
                .GetSection(DBUpSettings.ConfigurationSectionName)
                .Get<DBUpSettings>() ?? new DBUpSettings();

            migrationsSettings.Validate();

            builder.Services.AddSingleton(migrationsSettings);

            if (!migrationsSettings.Enabled)
            {
                return builder;
            }

            // DatabaseSettings must be registered by PersistenceSetupExtensions before migrations
            if (!builder.Services.Any(s => s.ServiceType == typeof(DatabaseSettings)))
            {
                throw new InvalidOperationException(
                    "DatabaseSettings is not registered in DI. Ensure AddPersistence() with database provider is called before AddDatabaseMigrations().");
            }

            return builder;
        }

        /// <summary>
        /// Runs database migrations on application startup.
        /// Uses DatabaseSettings from configuration for Host/Port/Database.
        /// Uses DBUpSettings for admin credentials.
        /// Supports both static connection strings and secret-based credentials.
        /// Tracking table stored in public.camus_schemaversions for portability.
        /// </summary>
        /// <param name="app">The web application.</param>
        /// <param name="logger">Logger instance for migration output.</param>
        /// <returns>The web application for method chaining.</returns>
        public static WebApplication UseDatabaseMigrations(this WebApplication app, ILogger logger)
        {
            // Skip migrations if disabled
            var migrationsSettings = app.Services.GetRequiredService<DBUpSettings>();
            if (!migrationsSettings.Enabled)
            {
                return app;
            }

            var dbSettings = app.Services.GetRequiredService<DatabaseSettings>();

            try
            {
                LogMigrationStarting(logger);

                // Get secret provider for credentials
                var secretProvider = app.Services.GetService<ISecretProvider>();
                if (secretProvider == null)
                {
                    throw new InvalidOperationException(
                        "Database migrations are configured to use secrets but ISecretProvider is not registered in DI");
                }

                LogCredentialsFetchFromSecrets(logger);

                // Fetch admin credentials from secret provider
                var adminUsername = secretProvider.GetSecret(migrationsSettings.AdminSecretName!);
                var adminPassword = secretProvider.GetSecret(migrationsSettings.PasswordSecretName!);

                if (string.IsNullOrWhiteSpace(adminUsername))
                {
                    throw new InvalidOperationException(
                        $"Database admin username secret '{migrationsSettings.AdminSecretName}' not found or empty");
                }

                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException(
                        $"Database admin password secret '{migrationsSettings.PasswordSecretName}' not found or empty");
                }

                // Build connection string using DatabaseSettings with admin credentials
                var connectionString = $"Host={dbSettings.Host};Port={dbSettings.Port};Database={dbSettings.Database};Username={adminUsername};Password={adminPassword}";

                if (!string.IsNullOrWhiteSpace(dbSettings.AdditionalParameters))
                {
                    connectionString += $";{dbSettings.AdditionalParameters}";
                }

                // Step 1: Configure DbUp to run migrations
                // Note: 001_initial_schema.sql creates the camus schema
                var upgrader = DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(
                        Assembly.GetExecutingAssembly(),
                        script => script.EndsWith(".sql", StringComparison.Ordinal))
                    .WithVariablesDisabled() // Disable variable substitution to avoid conflicts with bcrypt hashes ($2a$)
                    .JournalToPostgresqlTable(DefaultSchema, DefaultSchemaVersionsTable)
                    .LogToConsole()
                    .Build();

                // Step 2: Check if migrations are needed
                if (!upgrader.IsUpgradeRequired())
                {
                    LogDatabaseUpToDate(logger);
                    return app;
                }

                // Step 3: Execute migrations
                var result = upgrader.PerformUpgrade();

                if (!result.Successful)
                {
                    throw new InvalidOperationException($"Database migration failed: {result.Error.Message}", result.Error);
                }

                LogMigrationCompleted(logger);

                // Log which scripts were executed
                foreach (var script in result.Scripts)
                {
                    LogMigrationScriptExecuted(logger, script.Name);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run database migrations: {ex.Message}", ex);
            }

            return app;
        }
    }
}
