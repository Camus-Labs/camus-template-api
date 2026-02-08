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
    }
}
