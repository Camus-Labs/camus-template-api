using Swashbuckle.AspNetCore.Filters;
using emc.camus.api.Models.Responses;
using emc.camus.domain.Generic;

namespace emc.camus.api.SwaggerExamples
{
    /// <summary>
    /// Provides example data for ApiResponse&lt;ApiInfoResponse&gt; in Swagger documentation.
    /// </summary>
    public class ApiInfoExample : IExamplesProvider<ApiResponse<ApiInfoResponse>>
    {
        /// <summary>
        /// Returns an example ApiResponse&lt;ApiInfoResponse&gt; object for API documentation.
        /// </summary>
        /// <returns>Example API info response with sample data.</returns>
        public ApiResponse<ApiInfoResponse> GetExamples()
        {
            return new ApiResponse<ApiInfoResponse>
            {
                Message = "API information retrieved successfully",
                Data = new ApiInfoResponse
                {
                    Version = "2.0",
                    Status = "JWT Authentication",
                    Features = new List<string> 
                    { 
                        "Authentication (JWT & API Key)",
                        "Authorization (Role-based)",
                        "API Versioning (v1.0, v2.0)",
                        "OpenTelemetry Observability (Traces, Metrics, Logs)",
                        "Rate Limiting (In-Memory & Redis)",
                        "Swagger/OpenAPI Documentation",
                        "Dapr Secret Management",
                        "Structured Error Handling (ProblemDetails)",
                        "CORS Policy Configuration",
                        "Health Checks & Liveness Probes"
                    }
                },
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
