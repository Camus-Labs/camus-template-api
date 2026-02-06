using emc.camus.observability.otel;
using Asp.Versioning;
using System.Reflection;
using Microsoft.OpenApi;
using emc.camus.main.api.Handlers;
using System.Diagnostics;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using emc.camus.security.components;

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
builder.ConfigureCamusObservability(SERVICE_NAME, SERVICE_VERSION, INSTANCE_ID, ENV_NAME);

// Step 2: Add ActivitySource for manual tracing
var activitySource = new ActivitySource(SERVICE_NAME, SERVICE_VERSION);
builder.Services.AddSingleton(activitySource);

// Step 3: Add API versioning
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

// Step 4: Configure Swagger with versioning support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Camus API v1.0 Basic Demo",
        Version = "v1.0",
        Description = "Demo public endpoint."
    });
    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Camus API v2.0 Basic Security Demo",
        Version = "v2.0",
        Description = "Demo for private endpoints."
    });

    // Add JWT Bearer Security Definition (this is what creates the Authorize button)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Add ApiKey Security Definition (if you need it)
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. Example: 'ApiKey: {key}'",
        Name = "X-Api-Key", // or whatever header you use
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    // This makes the Authorize button apply to all endpoints by default
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    options.OperationFilter<DefaultApiResponsesOperationFilterHandler>();
    options.ExampleFilters();

    //Include swagger annotations
    options.EnableAnnotations();
});

// Step 5: Add CORS setup with configurable policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", cors =>
    {
        var corsConfig = builder.Configuration.GetSection("CorsSettings");
        cors.WithOrigins(corsConfig.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
            .WithMethods(corsConfig.GetSection("AllowedMethods").Get<string[]>() ?? Array.Empty<string>())
            .WithHeaders(corsConfig.GetSection("AllowedHeaders").Get<string[]>() ?? Array.Empty<string>())
            .WithExposedHeaders(corsConfig.GetSection("ExposedHeaders").Get<string[]>() ?? Array.Empty<string>())
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(corsConfig.GetValue<int>("PreflightMaxAgeMinutes", 60)));
    });
});

builder.Services.AddDependencyInjections(builder.Configuration);

// Step 6: Configure Security (Authentication & Authorization) using Camus Security Adapter
builder.AddCamusAuthentication();
builder.AddCamusAuthorization();

// Step 7: Add the controllers and build the app
builder.Services.AddControllers();

// Step 7: Build App Builder
var app = builder.Build();

// Step 8: Register X-Trace-Id header early
// Place response header BEFORE exception handling so exception logs include correlation IDs
app.UseMiddleware<ResponseTraceIdMiddleware>();

// Step 10: Global exception handling middleware (catches auth, authz, and app exceptions)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Step 11: Enable Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "API v2");
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

// Step # usings, caching and mappings
 await app.AppMappingsInjectionsAsync();

app.UseHttpsRedirection();
// Apply CORS before endpoints so all responses include the proper CORS headers
app.UseCors("ClientCors");

// Step 10: Add Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Step 11: Run the app
await app.RunAsync();