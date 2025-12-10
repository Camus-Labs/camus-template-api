using Serilog;

// Step 0.1: Create logger to capture all logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Step 2.3: Use Serilog for logging
builder.Host.UseSerilog();

// Step 4: Add the controllers and build the app
builder.Services.AddControllers();

// Step 4.1: Build App Builder
var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

// Step 9: Run the app
await app.RunAsync();