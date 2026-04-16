using emc.camus.observability.otel;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Extensions;
using emc.camus.cache.inmemory;
using emc.camus.security.jwt;
using emc.camus.security.apikey;
using emc.camus.secrets.dapr;
using emc.camus.documentation.swagger;
using emc.camus.ratelimiting.inmemory;
using emc.camus.migrations.dbup;

// Step 0: Define WebApplicationBuilder and settings
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
    options.ServicesStartConcurrently = true;
    options.ServicesStopConcurrently = true;
});

// Define service name for telemetry
string SERVICE_NAME = Assembly.GetExecutingAssembly().GetName().Name ?? "unknown-service-name";
// Get the service version from the assembly (matches <Version> in Directory.Build.props)
string SERVICE_VERSION = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown-service-version";
// Define a consistent instance id once and pass it to the adapter
string INSTANCE_ID = $"{Environment.MachineName}-{Environment.ProcessId}";
// Define environment name once for consistent resource attributes
string ENV_NAME = builder.Environment.EnvironmentName ?? "unknown-environment";

// Step 1: Configure logging + OpenTelemetry via adapter (with instance id and env name)
builder.AddObservability(SERVICE_NAME, SERVICE_VERSION, INSTANCE_ID, ENV_NAME);

// Step 2: Configure error handling (exception-to-error-code resolution and metrics)
builder.AddErrorHandling(SERVICE_NAME);

// Step 3: Configure request timeouts (prevents cascading failures from hanging calls)
builder.AddRequestTimeoutPolicies();

// Step 4: Configure API versioning
builder.AddApiVersioning();

// Step 5: Add Swagger documentation via adapter (depends on API versioning)
builder.AddSwaggerDocumentation();

// Step 6: Configure CORS policy
builder.AddCorsPolicy();

// Step 7: Configure rate limiting (always enabled for security)
builder.AddInMemoryRateLimiting(SERVICE_NAME);

// Step 8: Configure Secrets Provider using Dapr Adapter
builder.AddDaprSecrets();

// Step 9: Configure data persistence (depends on secrets for DB credentials)
builder.AddPersistence();

// Step 10: Configure database migrations (depends on persistence settings)
builder.AddDatabaseMigrations();

// Step 11: Configure in-memory cache (token revocation denylist)
builder.AddInMemoryCache();

// Step 12: Configure Authentication using Security Adapters (depends on secrets)
builder.AddJwtAuthentication();
builder.AddApiKeyAuthentication();

// Step 13: Configure authorization policies (depends on authentication)
builder.AddAuthorizationPolicies();

// Step 14: Configure application services (IUserContext, etc.)
builder.AddApplicationServices();

// Step 15: Configure health check services
builder.AddHealthChecks();

// Step 16: Build App Builder
var app = builder.Build();

// Step 17: Get logger for startup events
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

Program.LogServiceStarting(startupLogger, SERVICE_NAME, SERVICE_VERSION, ENV_NAME);
Program.LogInstanceId(startupLogger, INSTANCE_ID);

// Step 18: Configure transport security (forwarded headers, HSTS, HTTPS redirection)
app.UseTransportSecurity();

// Step 19: Add security headers (X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy)
app.UseSecurityHeaders();

// Step 20: Enable observability middleware (adds Trace Id header to responses)
// Must be BEFORE exception handling so trace IDs are available in error logs
app.UseObservability();

// Step 21: Global exception handling middleware (catches auth, authz, and app exceptions)
// Must be EARLY in pipeline to catch exceptions from rate limiting, auth, etc.
app.UseErrorHandling();

// Step 22: Apply request timeouts (AFTER error handling so timeout exceptions are caught)
app.UseRequestTimeoutPolicies();

// Step 23: Enable Swagger UI in development
app.UseSwaggerDocumentation();

// Step 24: Apply CORS policy (before authentication to allow preflight requests)
app.UseCorsPolicy();

// Step 25: Apply rate limiting (MUST be before authentication to prevent auth bypass attacks)
app.UseInMemoryRateLimiting();

// Step 26: Initialize Dapr secrets provider (fail-fast if secrets can't be loaded)
app.UseDaprSecrets();

// Step 27: Run database migrations (creates schema and tables if needed)
app.UseDatabaseMigrations(startupLogger);

// Step 28: Add Authentication and Authorization
app.UseAuthentication();

// Step 29: Apply authorization middleware
app.UseAuthorizationPolicies();

// Step 30: Initialize persistence-dependent data (load API info)
await app.UsePersistenceAsync();

// Step 31: Apply application services (adds Username header + endpoint routing)
app.UseApplicationServices();

// Step 32: Map health check endpoints (liveness, readiness, overall health)
app.UseHealthChecks();


Program.LogStartupComplete(startupLogger, SERVICE_NAME);

// Step 33: Run the app
await app.RunAsync();

/// <summary>
/// Application entry point. Partial class enables LoggerMessage source generation for top-level statements.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Starting {ServiceName} v{ServiceVersion} in {Environment} environment")]
    internal static partial void LogServiceStarting(ILogger logger, string serviceName, string serviceVersion, string environment);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Instance ID: {InstanceId}")]
    internal static partial void LogInstanceId(ILogger logger, string instanceId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "{ServiceName} startup complete. Ready to accept requests")]
    internal static partial void LogStartupComplete(ILogger logger, string serviceName);
}
