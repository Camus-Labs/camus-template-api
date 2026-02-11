using emc.camus.observability.otel;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using emc.camus.api.Extensions;
using emc.camus.api.Middleware;
using emc.camus.security.jwt;
using emc.camus.security.apikey;
using emc.camus.secrets.dapr;
using emc.camus.documentation.swagger;
using emc.camus.ratelimiting.memory;
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

// Step 2: Configure application services (versioning, CORS, authorization, controllers, business DI)
builder.AddApplicationServices();

// Step 3: Configure rate limiting (always enabled for security)
builder.AddMemoryRateLimiting(SERVICE_NAME);

// Step 4: Add Swagger documentation via adapter (depends on API versioning)
builder.AddSwaggerDocumentation();

// Step 5: Configure Secrets Provider using Dapr Adapter
builder.AddDaprSecrets();

// Step 6: Configure Authentication using Security Adapters
builder.AddJwtAuthentication(SERVICE_NAME);
builder.AddApiKeyAuthentication(SERVICE_NAME);

// Step 7: Build App Builder
var app = builder.Build();

// Get logger for startup events
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

startupLogger.LogInformation("Starting {ServiceName} v{ServiceVersion} in {Environment} environment", 
    SERVICE_NAME, SERVICE_VERSION, ENV_NAME);
startupLogger.LogInformation("Instance ID: {InstanceId}", INSTANCE_ID);

// Step 8: Configure forwarded headers for proxy/load balancer scenarios
// This ensures X-Forwarded-For and X-Real-IP headers are properly processed
// Critical for rate limiting to work correctly behind proxies (Azure LB, nginx, CloudFlare, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // In production, configure KnownProxies or KnownNetworks for security
    // For now, trust all proxies (suitable for Azure App Service, k8s ingress)
    ForwardLimit = null // Process all X-Forwarded-For entries
});

// Step 9: Enable observability middleware (adds Trace Id header to responses)
// Must be BEFORE exception handling so trace IDs are available in error logs
app.UseObservability();

// Step 10: Global exception handling middleware (catches auth, authz, and app exceptions)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Step 11: Apply rate limiting (MUST be before authentication to prevent auth bypass attacks)
app.UseMemoryRateLimiting();

// Step 12: Enable Swagger UI in development
app.UseSwaggerDocumentation();

// Step 13: Initialize Dapr secrets provider (fail-fast if secrets can't be loaded)
app.UseDaprSecrets();

app.UseHttpsRedirection();

// Step 14: Add Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Step 15: Apply application services (CORS, endpoint routing)
app.UseApplicationServices();

startupLogger.LogInformation("{ServiceName} startup complete. Ready to accept requests", SERVICE_NAME);

// Step 16: Run the app
await app.RunAsync();