using emc.camus.application.Auth;

namespace emc.camus.documentation.swagger.Configurations
{
    /// <summary>
    /// Configuration settings for Swagger/OpenAPI documentation.
    /// </summary>
    public class SwaggerSettings
    {
        /// <summary>
        /// The configuration section name for Swagger settings.
        /// </summary>
        public const string ConfigurationSectionName = "SwaggerSettings";

        /// <summary>
        /// Gets or sets whether Swagger should be enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the collection of API versions to document.
        /// </summary>
        public List<ApiVersionSettings> Versions { get; set; } = new();

        /// <summary>
        /// Gets or sets the security schemes to include in Swagger.
        /// </summary>
        public List<string> SecuritySchemes { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to include XML comments from the API assembly.
        /// </summary>
        public bool IncludeXmlComments { get; set; }

        /// <summary>
        /// Gets or sets whether to enable example filters.
        /// </summary>
        public bool EnableExampleFilters { get; set; }

        /// <summary>
        /// Validates the Swagger settings configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateVersions();
            ValidateSecuritySchemes();
        }

        private void ValidateVersions()
        {
            if (Versions == null)
                throw new InvalidOperationException("Versions cannot be null");

            if (!Enabled)
                return;

            if (Versions.Count == 0)
                throw new InvalidOperationException("At least one API version must be configured when Swagger is enabled");

            foreach (var version in Versions)
            {
                if (version == null)
                    throw new InvalidOperationException("Versions cannot contain null values");

                version.Validate();
            }
        }

        private void ValidateSecuritySchemes()
        {
            if (SecuritySchemes == null)
                throw new InvalidOperationException("SecuritySchemes cannot be null");

            if (!Enabled || SecuritySchemes.Count == 0)
                return;

            foreach (var scheme in SecuritySchemes)
            {
                if (string.IsNullOrWhiteSpace(scheme))
                    throw new InvalidOperationException("SecuritySchemes cannot contain null or empty values");

                var validSchemes = AuthenticationSchemes.GetAll();
                if (!validSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Invalid security scheme '{scheme}'. Valid values are: {string.Join(", ", validSchemes)} (case-insensitive)");
            }
        }
    }
}
