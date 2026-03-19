using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;

namespace emc.camus.api.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring API versioning services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ApiVersioningSetupExtensions
    {
        /// <summary>
        /// Registers API versioning services with URL segment and header-based version readers.
        /// Configures default version (1.0) and API explorer for documentation generation.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddApiVersioning(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Configure API versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("Api-Version")
                );
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            return builder;
        }
    }
}
