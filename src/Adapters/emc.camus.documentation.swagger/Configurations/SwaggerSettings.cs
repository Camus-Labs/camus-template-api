namespace emc.camus.documentation.swagger.Configurations
{
    /// <summary>
    /// Configuration settings for Swagger/OpenAPI documentation.
    /// </summary>
    public class SwaggerSettings
    {
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
        /// Gets or sets whether to enable Swagger annotations.
        /// </summary>
        public bool EnableAnnotations { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable example filters.
        /// </summary>
        public bool EnableExampleFilters { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to redirect root path to Swagger UI.
        /// </summary>
        public bool RedirectRootToSwagger { get; set; } = false;

        /// <summary>
        /// Validates the Swagger settings configuration.
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        public void Validate()
        {
            if (Versions == null)
                throw new ArgumentException("Versions cannot be null", nameof(Versions));

            // Only validate versions if Swagger is enabled
            if (Enabled)
            {
                if (Versions.Count == 0)
                    throw new ArgumentException("At least one API version must be configured when Swagger is enabled", nameof(Versions));

                // Validate each version
                foreach (var version in Versions)
                {
                    if (version == null)
                        throw new ArgumentException("Versions cannot contain null values", nameof(Versions));

                    version.Validate();
                }
            }

            if (SecuritySchemes == null)
                throw new ArgumentException("SecuritySchemes cannot be null", nameof(SecuritySchemes));

            // Validate security schemes if Swagger is enabled
            if (Enabled && SecuritySchemes.Count > 0)
            {
                var validSchemes = new[] { "bearer", "jwt", "apikey" };
                foreach (var scheme in SecuritySchemes)
                {
                    if (string.IsNullOrWhiteSpace(scheme))
                        throw new ArgumentException("SecuritySchemes cannot contain null or empty values", nameof(SecuritySchemes));

                    if (!validSchemes.Contains(scheme.ToLowerInvariant()))
                        throw new ArgumentException(
                            $"Invalid security scheme '{scheme}'. Valid values are: {string.Join(", ", validSchemes)}",
                            nameof(SecuritySchemes));
                }
            }
        }
    }
}
