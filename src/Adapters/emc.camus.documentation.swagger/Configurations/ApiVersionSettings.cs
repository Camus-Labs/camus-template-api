namespace emc.camus.documentation.swagger.Configurations
{
    /// <summary>
    /// Represents an API version to be documented in Swagger.
    /// </summary>
    public class ApiVersionSettings
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
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when any setting is invalid.
        /// </exception>
        public void Validate()
        {
            ValidateVersion();
            ValidateTitle();
            ValidateDescription();
        }

        private void ValidateVersion()
        {
            if (string.IsNullOrWhiteSpace(Version))
                throw new InvalidOperationException("Version cannot be null or empty");
        }

        private void ValidateTitle()
        {
            if (string.IsNullOrWhiteSpace(Title))
                throw new InvalidOperationException("Title cannot be null or empty");
        }

        private void ValidateDescription()
        {
            if (string.IsNullOrWhiteSpace(Description))
                throw new InvalidOperationException("Description cannot be null or empty");
        }
    }
}
