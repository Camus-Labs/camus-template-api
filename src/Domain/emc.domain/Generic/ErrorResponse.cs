namespace emc.camus.domain.Generic
{
    /// <summary>
    /// Represents a standard error response for API calls.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the status code of the error response.
        /// </summary>
        public int StatusCode { get; set; }


        /// <summary>
        /// Gets or sets the error message of the error response.
        /// </summary>
        public string? Message { get; set; }


        /// <summary>
        /// Gets or sets the detailed error information, if available. Only included in development for detailed errors.
        /// </summary>
        public string? Detail { get; set; }
    }
}