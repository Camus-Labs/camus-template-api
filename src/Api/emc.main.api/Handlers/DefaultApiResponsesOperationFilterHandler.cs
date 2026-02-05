using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace emc.camus.main.api.Handlers
{
    /// <summary>
    /// Operation filter to add default API responses for common error scenarios using ProblemDetails.
    /// </summary>
    public class DefaultApiResponsesOperationFilterHandler : IOperationFilter
    {
        
        /// <summary>
        /// Applies the operation filter to add default API responses.
        /// This includes responses for 400, 401, 403, and 500 status codes with a common schema.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);

            var examples = new Dictionary<string, OpenApiObject>
            {
                ["400"] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(400),
                    ["title"] = new OpenApiString("Bad Request"),
                    ["detail"] = new OpenApiString("A detailed error message describing what went wrong."),
                    ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7231#section-6.5.1")
                },
                ["401"] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(401),
                    ["title"] = new OpenApiString("Unauthorized"),
                    ["detail"] = new OpenApiString("Authentication is required and has failed or has not yet been provided."),
                    ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7235#section-3.1")
                },
                ["403"] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(403),
                    ["title"] = new OpenApiString("Forbidden"),
                    ["detail"] = new OpenApiString("You do not have permission to access this resource."),
                    ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7235#section-3.3")
                },
                ["500"] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(500),
                    ["title"] = new OpenApiString("Internal Server Error"),
                    ["detail"] = new OpenApiString("An unexpected error occurred on the server."),
                    ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7231#section-6.6.1")
                }
            };
            
            foreach (var status in new[] { "400", "401", "403", "500" })
            {
                operation.Responses[status] = new OpenApiResponse
                {
                    Description = status switch
                    {
                        "400" => "Bad Request",
                        "401" => "Unauthorized",
                        "403" => "Forbidden", 
                        "500" => "Internal Server Error",
                        _ => ""
                    },
                    Content = {
                        ["application/problem+json"] = new OpenApiMediaType
                        {
                            Schema = schema,
                            Example = examples[status]
                        }
                    }
                };
            }
        }
    }
}
