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
    public static class DatabaseMigrationExtensions
    {
        private const string DefaultSchema = "public";
        private const string DefaultSchemaVersionsTable = "camus_schemaversions";

        /// <summary>
        /// Registers database migration configuration and validates settings.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddDatabaseMigrations(this WebApplicationBuilder builder)
        {
            // Load and validate DBUpSettings
            var migrationsSettings = builder.Configuration
                .GetSection(DBUpSettings.ConfigurationSectionName)
                .Get<DBUpSettings>();

            if (migrationsSettings == null)
            {
                throw new InvalidOperationException(
                    $"DBUpSettings configuration is missing. Ensure '{DBUpSettings.ConfigurationSectionName}' section is present in configuration.");
            }

            migrationsSettings.Validate();
            builder.Services.AddSingleton(migrationsSettings);

            // Load and validate DatabaseSettings
            var dbSettings = builder.Configuration
                .GetSection(DatabaseSettings.ConfigurationSectionName)
                .Get<DatabaseSettings>();

            if (dbSettings == null)
            {
                throw new InvalidOperationException(
                    $"DatabaseSettings configuration is missing. Ensure '{DatabaseSettings.ConfigurationSectionName}' section is present in configuration.");
            }

            dbSettings.Validate();

            return builder;
        }

        /// <summary>
        /// Runs database migrations on application startup.
        /// Uses DatabaseSettings from root configuration for Host/Port/Database.
        /// Uses DBUpSettings for admin credentials.
        /// Supports both static connection strings and secret-based credentials.
        /// Tracking table stored in public.camus_schemaversions for portability.
        /// </summary>
        /// <param name="app">The web application.</param>
        /// <param name="logger">Logger instance for migration output.</param>
        /// <returns>The web application for method chaining.</returns>
        public static WebApplication UseDatabaseMigrations(this WebApplication app, ILogger logger)
        {
            // Get settings from DI (registered in AddDatabaseMigrations)
            var migrationsSettings = app.Services.GetRequiredService<DBUpSettings>();
            var dbSettings = app.Configuration
                .GetSection(DatabaseSettings.ConfigurationSectionName)
                .Get<DatabaseSettings>()!;

            try
            {
                logger.LogInformation("Starting database migrations...");

                // Get secret provider for credentials
                var secretProvider = app.Services.GetService<ISecretProvider>();
            if (secretProvider == null)
            {
                throw new InvalidOperationException(
                    "Database migrations are configured to use secrets but ISecretProvider is not registered in DI");
            }
            
            logger.LogInformation("Database migration credentials will be fetched from secret provider");

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

            // Build connection string using DatabaseSettings structure with admin credentials
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
                    script => script.EndsWith(".sql"))
                .WithVariablesDisabled() // Disable variable substitution to avoid conflicts with bcrypt hashes ($2a$)
                .JournalToPostgresqlTable(DefaultSchema, DefaultSchemaVersionsTable)
                .LogToConsole()
                .Build();

            // Step 2: Check if migrations are needed
            if (!upgrader.IsUpgradeRequired())
            {
                logger.LogInformation("Database is up to date. No migrations needed.");
                return app;
            }

            // Step 3: Execute migrations
            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                logger.LogError(result.Error, "Database migration failed");
                throw new InvalidOperationException("Database migration failed. See logs for details.", result.Error);
            }

            logger.LogInformation("Database migrations completed successfully");
            
            // Log which scripts were executed
            foreach (var script in result.Scripts)
            {
                logger.LogInformation("Executed migration: {ScriptName}", script.Name);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to run database migrations. Ensure the database is accessible and the configuration is correct.", ex);
        }

        return app;
    }
}
}