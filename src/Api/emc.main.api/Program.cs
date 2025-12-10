using Serilog;
using Asp.Versioning;

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

// Step 4: Add the controllers and build the app
builder.Services.AddControllers();

// Step 4.1: Build App Builder
var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

// Step 9: Run the app
await app.RunAsync();