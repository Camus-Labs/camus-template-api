using emc.camus.observability.otel;
using Asp.Versioning;
using System.Reflection;
using Microsoft.OpenApi;
using emc.camus.main.api.Handlers;
using System.Diagnostics;
using emc.camus.domain.Logging;
using emc.camus.main.api.Configurations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Text;

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

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

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

// Step 6.1: Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer()
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
    
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>, RsaSecurityKey>((options, jwtSettingsOptions, rsaKey) =>
    {
        var jwtSettings = jwtSettingsOptions.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = rsaKey,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Step 6: Add the controllers and build the app
builder.Services.AddControllers();

// Step 7: Build App Builder
var app = builder.Build();

// Step 8: Register X-Trace-Id header early
// Place response header BEFORE exception handling so exception logs include correlation IDs
app.UseMiddleware<ResponseTraceIdMiddleware>();

// Step 9: Global exception handling middleware (wraps everything that follows)
app.UseMiddleware<ExceptionHandlingMiddleware>();

//Step 10: Enable Swagger UI in development
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

// Step 10.1: Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Step 11: Run the app
await app.RunAsync();