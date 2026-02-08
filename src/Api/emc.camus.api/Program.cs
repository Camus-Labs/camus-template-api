using emc.camus.observability.otel;
using System.Reflection;
using emc.camus.api.Extensions;
using emc.camus.api.Middleware;
using emc.camus.security.jwt;
using emc.camus.security.apikey;
using emc.camus.secrets.dapr;
using emc.camus.documentation.swagger;

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

// Step 3: Add Swagger documentation via adapter (depends on API versioning)
builder.AddSwaggerDocumentation();

// Step 4: Configure Secrets Provider using Dapr Adapter
builder.AddDaprSecrets();

// Step 5: Configure Authentication using Security Adapters
builder.AddJwtAuthentication();
builder.AddApiKeyAuthentication();

// Step 6: Build App Builder
var app = builder.Build();

// Step 7: Enable observability middleware (adds X-Trace-Id header to responses)
// Must be BEFORE exception handling so trace IDs are available in error logs
app.UseObservability();

// Step 8: Global exception handling middleware (catches auth, authz, and app exceptions)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Step 9: Enable Swagger UI in development
app.UseSwaggerDocumentation();

// Step 10: Initialize Dapr secrets provider (fail-fast if secrets can't be loaded)
app.UseDaprSecrets();

app.UseHttpsRedirection();

// Step 11: Add Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Step 12: Apply application services (CORS, endpoint routing)
app.UseApplicationServices();

// Step 13: Run the app
await app.RunAsync();