using Microsoft.AspNetCore.Hosting;

namespace emc.camus.api.integration.test.Fixtures;

/// <summary>
/// Factory variant for integration testing with Swagger enabled.
/// Overrides the base environment to Development so that <c>UseSwaggerDocumentation</c>
/// serves the Swagger UI and OpenAPI JSON endpoints.
/// </summary>
public class ApiSwaggerFactory : ApiFactoryBase
{
    protected override void ConfigureVariantHostSettings(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("SwaggerSettings:Enabled", "true");
        builder.UseSetting("SwaggerSettings:Versions:0:Version", "v1");
        builder.UseSetting("SwaggerSettings:Versions:0:Title", "Camus API v1.0 Basic Demo");
        builder.UseSetting("SwaggerSettings:Versions:0:Description", "Demo public endpoint.");
        builder.UseSetting("SwaggerSettings:Versions:1:Version", "v2");
        builder.UseSetting("SwaggerSettings:Versions:1:Title", "Camus API v2.0 Basic Security Demo");
        builder.UseSetting("SwaggerSettings:Versions:1:Description", "Demo for private endpoints.");
        builder.UseSetting("SwaggerSettings:SecuritySchemes:0", "Bearer");
        builder.UseSetting("SwaggerSettings:SecuritySchemes:1", "ApiKey");
        builder.UseSetting("DataPersistenceSettings:Provider", "InMemory");
        builder.UseSetting("DBUpSettings:Enabled", "false");
    }
}
