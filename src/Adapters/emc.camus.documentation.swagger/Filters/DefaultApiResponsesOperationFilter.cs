using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using emc.camus.application.Generic;

namespace emc.camus.documentation.swagger.Filters
{
    /// <summary>
    /// Operation filter to add default API responses for common error scenarios using ProblemDetails.
    /// </summary>
    public class DefaultApiResponsesOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the operation filter to add default API responses.
        /// This includes responses for 400, 401, 403, 429, and 500 status codes with a common schema.
        /// </summary>
        /// <param name="operation">The OpenAPI operation to modify.</param>
        /// <param name="context">The operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);

            // Single examples for most status codes
            var examples = new Dictionary<string, OpenApiObject>
            {
                ["400"] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(400),
                    ["title"] = new OpenApiString("Bad Request"),
                    ["detail"] = new OpenApiString("A detailed error message describing what went wrong."),
                    ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7231#section-6.5.1"),
                    ["error"] = new OpenApiString(ErrorCodes.BadRequest)
                },
                ["403"] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(403),
                    ["title"] = new OpenApiString("Forbidden"),
                    ["detail"] = new OpenApiString("You do not have permission to access this resource."),
                    ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7235#section-3.3"),
                    ["error"] = new OpenApiString(ErrorCodes.Forbidden)
                },
                ["429"] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(429),
                    ["title"] = new OpenApiString("Too Many Requests"),
                    ["detail"] = new OpenApiString("Too many requests. Please try again later."),
                    ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc6585#section-4"),
                    ["error"] = new OpenApiString(ErrorCodes.RateLimitExceeded),
                    ["retryAfter"] = new OpenApiDouble(20.0)
                },
                ["500"] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(500),
                    ["title"] = new OpenApiString("Internal Server Error"),
                    ["detail"] = new OpenApiString("An unexpected error occurred on the server."),
                    ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7231#section-6.6.1"),
                    ["error"] = new OpenApiString(ErrorCodes.InternalServerError)
                }
            };

            // Multiple examples for 401 to show different error codes
            var unauthorizedExamples = new Dictionary<string, OpenApiExample>
            {
                ["Token Expired"] = new OpenApiExample
                {
                    Summary = "Token Expired",
                    Description = "JWT token has expired and needs to be refreshed",
                    Value = new OpenApiObject
                    {
                        ["status"] = new OpenApiInteger(401),
                        ["title"] = new OpenApiString("Unauthorized"),
                        ["detail"] = new OpenApiString("The token has expired."),
                        ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7235#section-3.1"),
                        ["error"] = new OpenApiString(ErrorCodes.Jwt.TokenExpired)
                    }
                },
                ["Invalid Token"] = new OpenApiExample
                {
                    Summary = "Invalid Token",
                    Description = "JWT token is malformed or invalid",
                    Value = new OpenApiObject
                    {
                        ["status"] = new OpenApiInteger(401),
                        ["title"] = new OpenApiString("Unauthorized"),
                        ["detail"] = new OpenApiString("The token is invalid."),
                        ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7235#section-3.1"),
                        ["error"] = new OpenApiString(ErrorCodes.Jwt.InvalidToken)
                    }
                },
                ["Invalid Signature"] = new OpenApiExample
                {
                    Summary = "Invalid Signature",
                    Description = "JWT token signature validation failed",
                    Value = new OpenApiObject
                    {
                        ["status"] = new OpenApiInteger(401),
                        ["title"] = new OpenApiString("Unauthorized"),
                        ["detail"] = new OpenApiString("The token signature is invalid."),
                        ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7235#section-3.1"),
                        ["error"] = new OpenApiString(ErrorCodes.Jwt.InvalidSignature)
                    }
                },
                ["Authentication Required"] = new OpenApiExample
                {
                    Summary = "Authentication Required",
                    Description = "No authentication token provided",
                    Value = new OpenApiObject
                    {
                        ["status"] = new OpenApiInteger(401),
                        ["title"] = new OpenApiString("Unauthorized"),
                        ["detail"] = new OpenApiString("Authentication is required to access this resource."),
                        ["type"] = new OpenApiString("https://tools.ietf.org/html/rfc7235#section-3.1"),
                        ["error"] = new OpenApiString(ErrorCodes.AuthenticationRequired)
                    }
                }
            };

            // Add 401 with multiple examples
            operation.Responses["401"] = new OpenApiResponse
            {
                Description = "Unauthorized",
                Content = {
                    ["application/problem+json"] = new OpenApiMediaType
                    {
                        Schema = schema,
                        Examples = unauthorizedExamples
                    }
                }
            };
            
            // Add other status codes with single examples
            var descriptions = new Dictionary<string, string>
            {
                ["400"] = "Bad Request",
                ["403"] = "Forbidden",
                ["429"] = "Too Many Requests",
                ["500"] = "Internal Server Error"
            };
            
            foreach (var status in descriptions.Keys)
            {
                operation.Responses[status] = new OpenApiResponse
                {
                    Description = descriptions[status],
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
