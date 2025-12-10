
namespace emc.camus.domain.Generic
{
    /// <summary>
    /// Represents a standard API response for successful or informational results.
    /// </summary>
    public class DefaultApiResponse
    {
        /// <summary>
        /// Gets or sets the status or result code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the main message for the response.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets additional details for the response, if available.
        /// </summary>
        public string? Detail { get; set; }
    }
}