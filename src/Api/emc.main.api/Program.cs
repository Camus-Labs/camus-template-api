using emc.camus.observability.otel.Logging;
using Asp.Versioning;
using System.Reflection;
using Microsoft.OpenApi;
using emc.camus.main.api.Handlers;
using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using emc.camus.domain.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;

// Define service name for telemetry
string SERVICE_NAME = Assembly.GetExecutingAssembly().GetName().Name ?? "unknown-service";
// Get the service version from the assembly (matches <Version> in csproj)
string SERVICE_VERSION = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown-version";

// Step 0: Bootstrap logger (console only) to capture early logs
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseEmcSerilog(
    builder.Configuration,
    builder.Environment,
    SERVICE_NAME,
    SERVICE_VERSION
);


// Step 1: Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version")
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Step 2: Configure Swagger with versioning support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Camus API v1.0 Basic Demo",
        Version = "v1.0",
        Description = "A sample API demonstrating basic features"
    });
    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Camus API v2.0 Multi-version API configuration",
        Version = "v2.0",
        Description = "A sample API demonstrating multi-versioning and documentation"
    });
    options.SwaggerDoc("v3", new OpenApiInfo
    {
        Title = "Camus API v3.0 Documented API configuration",
        Version = "v3.0",
        Description = "Camus API v3.0 demonstrating logging and telemetry"
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    //Include swagger annotations
    options.EnableAnnotations();
});

// Step 2.1: Add observability with OpenTelemetry
var openTelemetryConfig = builder.Configuration.GetSection("OpenTelemetry");
var selectedTracingExporter = openTelemetryConfig["Tracing:Exporter"] ?? "none";
var selectedMetricsExporter = openTelemetryConfig["Metrics:Exporter"] ?? "none";

var activitySource = new ActivitySource(SERVICE_NAME, SERVICE_VERSION);
builder.Services.AddSingleton(activitySource);
builder.Services.AddSingleton<IActivitySourceWrapper, ActivitySourceWrapper>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: SERVICE_NAME,
                        serviceVersion: SERVICE_VERSION
                    )
                    .AddAttributes(
                        [
                            new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName ?? "unknown")
                        ]
                    )
            )
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    // Always record authentication status as a boolean (true/false)
                    var isAuthenticated = request.HttpContext.User?.Identity?.IsAuthenticated ?? false;
                    activity.SetTag("enduser.authenticated", isAuthenticated);

                    // Add end-user identity only if available (avoid empty/whitespace)
                    var endUser = request.HttpContext.User?.Identity?.Name;
                    if (!string.IsNullOrWhiteSpace(endUser))
                    {
                        activity.SetTag("enduser.id", endUser);
                    }
                };
                options.EnrichWithHttpResponse = (activity, response) =>
                {
                    var routeData = response.HttpContext.GetRouteData();
                    var controller = routeData?.Values["controller"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(controller))
                    {
                        activity.SetTag("http.route.controller", controller);
                    }

                    var version = routeData?.Values["version"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        activity.SetTag("http.route.version", version);
                    }
                };
            })
            .AddHttpClientInstrumentation();
        ConfigureTracingExporter(tracerProviderBuilder, selectedTracingExporter, openTelemetryConfig);
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: SERVICE_NAME,
                        serviceVersion: SERVICE_VERSION
                    )
                    .AddAttributes(
                        [
                            new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName ?? "unknown")
                        ]
                    )
            )
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation();
        ConfigureMetricsExporter(meterProviderBuilder, selectedMetricsExporter, openTelemetryConfig);
    });

// Step 3: Add CORS setup with configurable policy
builder.Services.AddCors(options =>
{
    var corsConfig = builder.Configuration.GetSection("CorsSettings");
    options.AddPolicy("ClientCors", cors =>
    {
        cors.WithOrigins(corsConfig.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
            .WithMethods(corsConfig.GetSection("AllowedMethods").Get<string[]>() ?? Array.Empty<string>())
            .WithHeaders(corsConfig.GetSection("AllowedHeaders").Get<string[]>() ?? Array.Empty<string>())
            .WithExposedHeaders(corsConfig.GetSection("ExposedHeaders").Get<string[]>() ?? Array.Empty<string>())
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(corsConfig.GetValue<int>("PreflightMaxAgeMinutes", 60)));
    });
});

// Step 4: Add the controllers and build the app
builder.Services.AddControllers();

// Step 5: Build App Builder
var app = builder.Build();

// Step 6: Register X-Trace-Id header early
// Place response header BEFORE exception handling so exception logs include correlation IDs
app.UseMiddleware<ResponseTraceIdMiddleware>();

// Step 7: Global exception handling middleware (wraps everything that follows)
app.UseMiddleware<ExceptionHandlingMiddleware>();

//Step 8: Enable Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "API v2");
        options.SwaggerEndpoint("/swagger/v3/swagger.json", "API v3");
    });

    //Redirect to Swagger UI if no path is specified
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/" || context.Request.Path == string.Empty)
        {
            context.Response.Redirect("/swagger/index.html", permanent: false);
            return;
        }
        await next();
    });
}

app.UseHttpsRedirection();
// Apply CORS before endpoints so all responses include the proper CORS headers
app.UseCors("ClientCors");
app.MapControllers();

// Step 9: Run the app
await app.RunAsync();

// Helper methods to reduce cognitive complexity while maintaining exact functionality
static void ConfigureTracingExporter(
    TracerProviderBuilder tracerProviderBuilder,
    string selectedExporter,
    IConfiguration openTelemetryConfig)
{
    switch (selectedExporter.ToLowerInvariant())
    {
        case "otlp":
            tracerProviderBuilder.AddOtlpExporter(options =>
            {
                var endpoint = openTelemetryConfig["Tracing:OtlpEndpoint"];
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    options.Endpoint = new Uri(endpoint);
                }
                // Use OTLP over gRPC (port 4317)
                options.Protocol = OtlpExportProtocol.Grpc;
            });
            break;

        case "console":
            tracerProviderBuilder.AddConsoleExporter();
            break;

        default:
            // No provider
            break;
    }
}


static void ConfigureMetricsExporter(
    MeterProviderBuilder meterProviderBuilder,
    string selectedExporter,
    IConfiguration openTelemetryConfig)
{
    switch (selectedExporter.ToLowerInvariant())
    {
        case "otlp":
            meterProviderBuilder.AddOtlpExporter(options =>
            {
                var endpoint = openTelemetryConfig["Metrics:OtlpEndpoint"];
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    options.Endpoint = new Uri(endpoint);
                }
                // Use OTLP over gRPC (port 4317)
                options.Protocol = OtlpExportProtocol.Grpc;
            });
            break;

        case "console":
            meterProviderBuilder.AddConsoleExporter();
            break;

        default:
            // No provider
            break;
    }
}