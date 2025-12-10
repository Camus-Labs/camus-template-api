var builder = WebApplication.CreateBuilder(args);

// Step 4: Add the controllers and build the app
builder.Services.AddControllers();

// Step 4.1: Build App Builder
var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

// Step 9: Run the app
await app.RunAsync();