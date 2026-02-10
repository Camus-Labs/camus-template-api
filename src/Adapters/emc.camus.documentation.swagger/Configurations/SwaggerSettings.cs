using emc.camus.application.Auth;

namespace emc.camus.documentation.swagger.Configurations
{
    /// <summary>
    /// Configuration settings for Swagger/OpenAPI documentation.
    /// </summary>
    public class SwaggerSettings
    {
        /// <summary>
        /// Valid security scheme names supported by Swagger configuration (case-insensitive).
        /// Accepts: "Bearer", or "ApiKey" in any case.
        /// </summary>
        private static readonly string[] ValidSecuritySchemes = 
        { 
            AuthenticationSchemes.JwtBearer, 
            AuthenticationSchemes.ApiKey 
        };
        /// <summary>
        /// Gets or sets whether Swagger should be enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the collection of API versions to document.
        /// </summary>
        public List<ApiVersionInfo> Versions { get; set; } = new();

        /// <summary>
        /// Gets or sets the security schemes to include in Swagger.
        /// </summary>
        public List<string> SecuritySchemes { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to include XML comments from the API assembly.
        /// </summary>
        public bool IncludeXmlComments { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable example filters.
        /// </summary>
        public bool EnableExampleFilters { get; set; } = false;

        /// <summary>
        /// Validates the Swagger settings configuration.
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        public void Validate()
        {
            ValidateVersions();
            ValidateSecuritySchemes();
        }

        private void ValidateVersions()
        {
            if (Versions == null)
                throw new ArgumentException("Versions cannot be null", nameof(Versions));

            if (!Enabled)
                return;

            if (Versions.Count == 0)
                throw new ArgumentException("At least one API version must be configured when Swagger is enabled", nameof(Versions));

            foreach (var version in Versions)
            {
                ValidateVersion(version);
            }
        }

        private void ValidateVersion(ApiVersionInfo version)
        {
            if (version == null)
                throw new ArgumentException("Versions cannot contain null values", nameof(Versions));

            version.Validate();
        }

        private void ValidateSecuritySchemes()
        {
            if (SecuritySchemes == null)
                throw new ArgumentException("SecuritySchemes cannot be null", nameof(SecuritySchemes));

            if (!Enabled || SecuritySchemes.Count == 0)
                return;

            foreach (var scheme in SecuritySchemes)
            {
                ValidateSecurityScheme(scheme, ValidSecuritySchemes);
            }
        }

        private void ValidateSecurityScheme(string scheme, string[] validSchemes)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                throw new ArgumentException("SecuritySchemes cannot contain null or empty values", nameof(SecuritySchemes));

            if (!validSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Invalid security scheme '{scheme}'. Valid values are: {string.Join(", ", validSchemes)} (case-insensitive)",
                    nameof(SecuritySchemes));
        }
    }
}
