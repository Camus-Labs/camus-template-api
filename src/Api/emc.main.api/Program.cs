using Serilog;
using Asp.Versioning;
using System.Reflection;
using Microsoft.OpenApi;

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
    options.SwaggerDoc("v1.0", new OpenApiInfo
    {
        Title = "Camus Basic API demo",
        Version = "v1.0",
        Description = "A sample API demonstrating basic features"
    });
    options.SwaggerDoc("v2.0", new OpenApiInfo
    {
        Title = "Camus Multi-version API configuration",
        Version = "v2.0",
        Description = "A sample API demonstrating multi-versioning"
    });
     options.SwaggerDoc("v3.0", new OpenApiInfo
    {
        Title = "Camus Documented API configuration",
        Version = "v3.0",
        Description = "A sample API demonstrating documentation"
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    //Include swagger annotations
    options.EnableAnnotations();
});

// Step 4: Add the controllers and build the app
builder.Services.AddControllers();

// Step 4.1: Build App Builder
var app = builder.Build();

//Step 6: Enable Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Camus Basic API demo v1.0");
        options.SwaggerEndpoint("/swagger/v2.0/swagger.json", "Camus Multi-version API configuration v2.0");
        options.SwaggerEndpoint("/swagger/v3.0/swagger.json", "Camus Documented API configuration v3.0");
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