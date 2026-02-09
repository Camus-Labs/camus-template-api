namespace emc.camus.documentation.swagger.Configurations
{
    /// <summary>
    /// Represents an API version to be documented in Swagger.
    /// </summary>
    public class ApiVersionInfo
    {
        /// <summary>
        /// Gets or sets the version identifier (e.g., "v1", "v2").
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display title for this version.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description for this version.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Validates the API version info configuration.
        /// Throws ArgumentException if any setting is invalid.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Version))
                throw new ArgumentException("Version cannot be null or empty", nameof(Version));

            if (string.IsNullOrWhiteSpace(Title))
                throw new ArgumentException("Title cannot be null or empty", nameof(Title));
        }
    }
}
