using Serilog;
using Asp.Versioning;
using System.Reflection;
using Microsoft.OpenApi;
using emc.camus.main.api.Handlers;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using emc.camus.domain.Logging;

// Define service name for telemetry
string SERVICE_NAME = Assembly.GetExecutingAssembly().GetName().Name ?? "unknown-service";
// Get the service version from the assembly (matches <Version> in csproj)
string SERVICE_VERSION = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown-version";

// Step 0: Create logger to capture all logs and start app building
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

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
var activitySource = new ActivitySource(SERVICE_NAME);
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
                    activity.SetTag("http.route.controller", response.HttpContext.GetRouteData()?.Values["controller"]?.ToString());
                    activity.SetTag("http.route.version", response.HttpContext.GetRouteData()?.Values["version"]?.ToString());
                };
            })
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

// Step 4: Add the controllers and build the app
builder.Services.AddControllers();

// Step 5: Build App Builder
var app = builder.Build();

// Step 6: Global exception handling middleware
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
app.MapControllers();

// Step 9: Run the app
await app.RunAsync();