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
using Microsoft.AspNetCore.HttpOverrides;

[assembly: ExcludeFromCodeCoverage]

// Step 0: Define WebApplicationBuilder and settings
var builder = WebApplication.CreateBuilder(args);

// Define service name for telemetry
string SERVICE_NAME = Assembly.GetExecutingAssembly().GetName().Name ?? "unknown-service-name";
// Get the service version from the assembly (matches <Version> in csproj)
string SERVICE_VERSION = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown-service-version";
// Define a consistent instance id once and pass it to the adapter
string INSTANCE_ID = $"{Environment.MachineName}-{Environment.ProcessId}";
// Define environment name once for consistent resource attributes
string ENV_NAME = builder.Environment.EnvironmentName ?? "unknown-environment";

// Step 1: Configure logging + OpenTelemetry via adapter (with instance id and env name)
builder.AddObservability(SERVICE_NAME, SERVICE_VERSION, INSTANCE_ID, ENV_NAME);

// Step 2: Configure error handling (exception-to-error-code resolution and metrics)
builder.AddErrorHandling(SERVICE_NAME);

// Step 3: Configure API versioning
builder.AddApiVersioning();

// Step 4: Add Swagger documentation via adapter (depends on API versioning)
builder.AddSwaggerDocumentation();

// Step 5: Configure CORS policy
builder.AddCorsPolicy();

// Step 6: Configure rate limiting (always enabled for security)
builder.AddInMemoryRateLimiting(SERVICE_NAME);

// Step 7: Configure Secrets Provider using Dapr Adapter
builder.AddDaprSecrets();

// Step 8: Configure database migrations (depends on secrets for DB credentials)
builder.AddDatabaseMigrations();

// Step 9: Configure in-memory cache (token revocation denylist)
builder.AddInMemoryCache();

// Step 10: Configure Authentication using Security Adapters (depends on secrets)
builder.AddJwtAuthentication();
builder.AddApiKeyAuthentication();

// Step 11: Configure authorization policies and user repository (depends on authentication)
builder.AddAuthorizationWithData();

// Step 12: Configure application services (IUserContext, etc.)
builder.AddApplicationServices();

// Step 13: Configure application data (depends on IUserContext for audit)
builder.AddAppData();

// Step 14: Build App Builder
var app = builder.Build();

// Step 15: Get logger for startup events
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

Program.LogServiceStarting(startupLogger, SERVICE_NAME, SERVICE_VERSION, ENV_NAME);
Program.LogInstanceId(startupLogger, INSTANCE_ID);

// Step 16: Configure forwarded headers for proxy/load balancer scenarios
// This ensures X-Forwarded-For and X-Real-IP headers are properly processed
// Critical for rate limiting to work correctly behind proxies (Azure LB, nginx, CloudFlare, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // In production, configure KnownProxies or KnownNetworks for security
    // For now, trust all proxies (suitable for Azure App Service, k8s ingress)
    ForwardLimit = null // Process all X-Forwarded-For entries
});

// Step 17: Enforce HTTPS (redirect HTTP to HTTPS)
app.UseHttpsRedirection();

// Step 18: Enable observability middleware (adds Trace Id header to responses)
// Must be BEFORE exception handling so trace IDs are available in error logs
app.UseObservability();

// Step 19: Global exception handling middleware (catches auth, authz, and app exceptions)
// Must be EARLY in pipeline to catch exceptions from rate limiting, auth, etc.
app.UseErrorHandling();

// Step 20: Enable Swagger UI in development
app.UseSwaggerDocumentation();

// Step 21: Apply CORS policy (before authentication to allow preflight requests)
app.UseCorsPolicy();

// Step 22: Apply rate limiting (MUST be before authentication to prevent auth bypass attacks)
app.UseInMemoryRateLimiting();

// Step 23: Initialize Dapr secrets provider (fail-fast if secrets can't be loaded)
app.UseDaprSecrets();

// Step 24: Run database migrations (creates schema and tables if needed)
app.UseDatabaseMigrations(startupLogger);

// Step 25: Add Authentication and Authorization
app.UseAuthentication();

// Step 26: Initialize authorization data (load users/roles)
app.UseAuthorizationWithData();

// Step 27: Initialize application data (load API info)
app.UseAppData();

// Step 28: Apply application services (adds User-Id header + endpoint routing)
app.UseApplicationServices();


Program.LogStartupComplete(startupLogger, SERVICE_NAME);

// Step 28: Run the app
await app.RunAsync();

/// <summary>
/// Application entry point. Partial class enables LoggerMessage source generation for top-level statements.
/// </summary>
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
