using emc.camus.observability.otel;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Extensions;
using emc.camus.api.Middleware;
using emc.camus.security.jwt;
using emc.camus.security.apikey;
using emc.camus.secrets.dapr;
using emc.camus.documentation.swagger;
using emc.camus.ratelimiting.inmemory;
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
builder.AddMemoryRateLimiting(SERVICE_NAME);

// Step 7: Configure Secrets Provider using Dapr Adapter
builder.AddDaprSecrets();

// Step 8: Configure Authentication using Security Adapters (depends on secrets)
builder.AddJwtAuthentication();
builder.AddApiKeyAuthentication();

// Step 9: Configure authorization policies and user repository (depends on authentication)
builder.AddAuthorization();

// Step 10: Configure application data (API info, etc.)
builder.AddAppData();

// Step 11: Configure controllers (uses all services above)
builder.AddApplicationServices();

// Step 12: Build App Builder
var app = builder.Build();

// Step 13: Get logger for startup events
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

startupLogger.LogInformation("Starting {ServiceName} v{ServiceVersion} in {Environment} environment", 
    SERVICE_NAME, SERVICE_VERSION, ENV_NAME);
startupLogger.LogInformation("Instance ID: {InstanceId}", INSTANCE_ID);

// Step 14: Configure forwarded headers for proxy/load balancer scenarios
// This ensures X-Forwarded-For and X-Real-IP headers are properly processed
// Critical for rate limiting to work correctly behind proxies (Azure LB, nginx, CloudFlare, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // In production, configure KnownProxies or KnownNetworks for security
    // For now, trust all proxies (suitable for Azure App Service, k8s ingress)
    ForwardLimit = null // Process all X-Forwarded-For entries
});

// Step 15: Enforce HTTPS (redirect HTTP to HTTPS)
app.UseHttpsRedirection();

// Step 16: Enable observability middleware (adds Trace Id header to responses)
// Must be BEFORE exception handling so trace IDs are available in error logs
app.UseObservability();

// Step 17: Global exception handling middleware (catches auth, authz, and app exceptions)
// Must be EARLY in pipeline to catch exceptions from rate limiting, auth, etc.
app.UseErrorHandling();

// Step 18: Enable Swagger UI in development
app.UseSwaggerDocumentation();

// Step 19: Apply CORS policy (before authentication to allow preflight requests)
app.UseCorsPolicy();

// Step 20: Apply rate limiting (MUST be before authentication to prevent auth bypass attacks)
app.UseMemoryRateLimiting();

// Step 21: Initialize Dapr secrets provider (fail-fast if secrets can't be loaded)
app.UseDaprSecrets();

// Step 22: Add Authentication and Authorization
app.UseAuthentication();

// Step 23: Initialize authorization data (load users/roles)
app.UseAuthorizationSetup();

// Step 24: Initialize application data (load API info)
app.UseAppDataSetup();

// Step 25: Apply application services (endpoint routing)
app.UseApplicationServices();


startupLogger.LogInformation("{ServiceName} startup complete. Ready to accept requests", SERVICE_NAME);

// Step 26: Run the app
await app.RunAsync();