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
        /// The name of the HTTP header used to specify the API version.
        /// </summary>
        private const string ApiVersionHeaderName = "Api-Version";

        /// <summary>
        /// The format string for API version group names in documentation.
        /// </summary>
        private const string ApiVersionGroupNameFormat = "'v'VVV";

        /// <summary>
        /// Registers API versioning services with URL segment and header-based version readers.
        /// Configures default version (1.0) and API explorer for documentation generation.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <returns>The web application builder for method chaining.</returns>
        public static WebApplicationBuilder AddApiVersioning(this WebApplicationBuilder builder)
        {
            // Configure API versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader(ApiVersionHeaderName)
                );
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = ApiVersionGroupNameFormat;
                options.SubstituteApiVersionInUrl = true;
            });

            return builder;
        }
    }
}
