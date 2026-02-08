using Swashbuckle.AspNetCore.Filters;
using emc.camus.domain.Auth;
using emc.camus.domain.Generic;

namespace emc.camus.api.SwaggerExamples
{
    /// <summary>
    /// Provides example data for ApiResponse&lt;ApiInfo&gt; in Swagger documentation.
    /// </summary>
    public class ApiInfoExample : IExamplesProvider<ApiResponse<ApiInfo>>
    {
        /// <summary>
        /// Returns an example ApiResponse&lt;ApiInfo&gt; object for API documentation.
        /// </summary>
        /// <returns>Example API info response with sample data.</returns>
        public ApiResponse<ApiInfo> GetExamples()
        {
            return new ApiResponse<ApiInfo>
            {
                Message = "API information retrieved successfully",
                Data = new ApiInfo
                {
                    Name = "My Basic API",
                    Version = "1.0",
                    Status = "Running with API Versioning v1.0",
                    Features = new List<string> 
                    { 
                        "Logging", 
                        "Versioning", 
                        "Authentication", 
                        "Authorization", 
                        "Observability" 
                    }
                },
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
