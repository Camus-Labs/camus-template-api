using emc.camus.observability.otel;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Extensions;
using emc.camus.cache.inmemory;
using emc.camus.security.jwt;
using emc.camus.security.apikey;
using emc.camus.secrets.dapr;
using emc.camus.documentation.swagger;
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
    options.ShutdownTimeout = TimeSpan.FromSeconds(Program.ShutdownTimeoutSeconds);
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

// Step 4: Configure idempotency key validation
builder.AddIdempotency(SERVICE_NAME);

// Step 5: Configure API versioning
builder.AddApiVersioning();

// Step 6: Add Swagger documentation via adapter (depends on API versioning)
builder.AddSwaggerDocumentation();

// Step 7: Configure CORS policy
builder.AddCorsPolicy();

// Step 8: Configure rate limiting (always enabled for security)
builder.AddRateLimiting(SERVICE_NAME);

// Step 9: Configure Secrets Provider using Dapr Adapter
builder.AddDaprSecrets();

// Step 10: Configure data persistence (depends on secrets for DB credentials)
builder.AddPersistence();

// Step 11: Configure database migrations (depends on persistence settings)
builder.AddDatabaseMigrations();

// Step 12: Configure in-memory cache (token revocation denylist)
builder.AddInMemoryCache();

// Step 13: Configure Authentication using Security Adapters (depends on secrets)
builder.AddJwtAuthentication();
builder.AddApiKeyAuthentication();

// Step 14: Configure authorization policies (depends on authentication)
builder.AddAuthorizationPolicies();

// Step 15: Configure application services (IUserContext, etc.)
builder.AddApplicationServices();

// Step 16: Configure health check services
builder.AddHealthChecks();

// Step 17: Build App Builder
var app = builder.Build();

// Step 18: Get logger for startup events
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

Program.LogServiceStarting(startupLogger, SERVICE_NAME, SERVICE_VERSION, ENV_NAME);
Program.LogInstanceId(startupLogger, INSTANCE_ID);

// Step 19: Configure transport security (forwarded headers, HSTS, HTTPS redirection)
app.UseTransportSecurity();

// Step 20: Add security headers (X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy)
app.UseSecurityHeaders();

// Step 21: Enable observability middleware (adds Trace Id header to responses)
// Must be BEFORE exception handling so trace IDs are available in error logs
app.UseObservability();

// Step 22: Apply request timeouts (BEFORE error handling so timeout exceptions propagate to it)
app.UseRequestTimeoutPolicies();

// Step 23: Global exception handling middleware (catches auth, authz, timeout, and app exceptions)
// Must be EARLY in pipeline to catch exceptions from rate limiting, auth, etc.
app.UseErrorHandling();

// Step 24: Enable Swagger UI in development
app.UseSwaggerDocumentation();

// Step 25: Apply CORS policy (before authentication to allow preflight requests)
app.UseCorsPolicy();

// Step 26: Apply rate limiting (MUST be before authentication to prevent auth bypass attacks)
app.UseRateLimiting();

// Step 27: Initialize Dapr secrets provider (fail-fast if secrets can't be loaded)
app.UseDaprSecrets();

// Step 28: Run database migrations (creates schema and tables if needed)
app.UseDatabaseMigrations(startupLogger);

// Step 29: Add Authentication and Authorization
app.UseAuthentication();

// Step 30: Apply authorization middleware
app.UseAuthorizationPolicies();

// Step 31: Initialize persistence-dependent data (load API info)
await app.UsePersistenceAsync();

// Step 32: Apply application services (adds Username header + endpoint routing)
app.UseApplicationServices();

// Step 33: Map health check endpoints (liveness, readiness, overall health)
app.UseHealthChecks();


Program.LogStartupComplete(startupLogger, SERVICE_NAME);

// Step 34: Run the app
await app.RunAsync();

/// <summary>
/// Application entry point. Partial class enables LoggerMessage source generation for top-level statements.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program
{
    private const int ShutdownTimeoutSeconds = 30;

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
