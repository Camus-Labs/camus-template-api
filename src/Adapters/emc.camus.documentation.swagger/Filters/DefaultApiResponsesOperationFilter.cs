using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using emc.camus.application.Common;

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
                [StatusCodes.Status400BadRequest.ToString(CultureInfo.InvariantCulture)] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(StatusCodes.Status400BadRequest),
                    ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)),
                    ["detail"] = new OpenApiString("A detailed error message describing what went wrong."),
                    ["error"] = new OpenApiString(ErrorCodes.BadRequest)
                },
                [StatusCodes.Status403Forbidden.ToString(CultureInfo.InvariantCulture)] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(StatusCodes.Status403Forbidden),
                    ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status403Forbidden)),
                    ["detail"] = new OpenApiString("You do not have permission to access this resource."),
                    ["error"] = new OpenApiString(ErrorCodes.Forbidden)
                },
                [StatusCodes.Status404NotFound.ToString(CultureInfo.InvariantCulture)] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(StatusCodes.Status404NotFound),
                    ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)),
                    ["detail"] = new OpenApiString("The requested resource was not found."),
                    ["error"] = new OpenApiString(ErrorCodes.NotFound)
                },
                [StatusCodes.Status409Conflict.ToString(CultureInfo.InvariantCulture)] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(StatusCodes.Status409Conflict),
                    ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)),
                    ["detail"] = new OpenApiString("The request conflicts with the current state of the resource."),
                    ["error"] = new OpenApiString(ErrorCodes.Conflict)
                },
                [StatusCodes.Status429TooManyRequests.ToString(CultureInfo.InvariantCulture)] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(StatusCodes.Status429TooManyRequests),
                    ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status429TooManyRequests)),
                    ["detail"] = new OpenApiString("Too many requests. Please try again later."),
                    ["error"] = new OpenApiString(ErrorCodes.RateLimitExceeded),
                    ["retryAfter"] = new OpenApiDouble(20.0)
                },
                [StatusCodes.Status500InternalServerError.ToString(CultureInfo.InvariantCulture)] = new OpenApiObject
                {
                    ["status"] = new OpenApiInteger(StatusCodes.Status500InternalServerError),
                    ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)),
                    ["detail"] = new OpenApiString("An unexpected error occurred on the server."),
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
                        ["status"] = new OpenApiInteger(StatusCodes.Status401Unauthorized),
                        ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized)),
                        ["detail"] = new OpenApiString("You are not authorized to access this resource."),
                        ["error"] = new OpenApiString(ErrorCodes.JwtTokenExpired)
                    }
                },
                ["Invalid Token"] = new OpenApiExample
                {
                    Summary = "Invalid Token",
                    Description = "JWT token is malformed or invalid",
                    Value = new OpenApiObject
                    {
                        ["status"] = new OpenApiInteger(StatusCodes.Status401Unauthorized),
                        ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized)),
                        ["detail"] = new OpenApiString("You are not authorized to access this resource."),
                        ["error"] = new OpenApiString(ErrorCodes.JwtInvalidToken)
                    }
                },
                ["Invalid Signature"] = new OpenApiExample
                {
                    Summary = "Invalid Signature",
                    Description = "JWT token signature validation failed",
                    Value = new OpenApiObject
                    {
                        ["status"] = new OpenApiInteger(StatusCodes.Status401Unauthorized),
                        ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized)),
                        ["detail"] = new OpenApiString("You are not authorized to access this resource."),
                        ["error"] = new OpenApiString(ErrorCodes.JwtInvalidSignature)
                    }
                },
                ["Authentication Required"] = new OpenApiExample
                {
                    Summary = "Authentication Required",
                    Description = "No authentication token provided",
                    Value = new OpenApiObject
                    {
                        ["status"] = new OpenApiInteger(StatusCodes.Status401Unauthorized),
                        ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized)),
                        ["detail"] = new OpenApiString("You are not authorized to access this resource."),
                        ["error"] = new OpenApiString(ErrorCodes.AuthenticationRequired)
                    }
                }
            };

            // Add 401 with multiple examples
            operation.Responses[StatusCodes.Status401Unauthorized.ToString(CultureInfo.InvariantCulture)] = new OpenApiResponse
            {
                Description = ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized),
                Content = {
                    [MediaTypes.ProblemJson] = new OpenApiMediaType
                    {
                        Schema = schema,
                        Examples = unauthorizedExamples
                    }
                }
            };
            
            // Add other status codes with single examples
            var statusCodesToAdd = new Dictionary<string, string>
            {
                [StatusCodes.Status400BadRequest.ToString(CultureInfo.InvariantCulture)] = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
                [StatusCodes.Status403Forbidden.ToString(CultureInfo.InvariantCulture)] = ReasonPhrases.GetReasonPhrase(StatusCodes.Status403Forbidden),
                [StatusCodes.Status404NotFound.ToString(CultureInfo.InvariantCulture)] = ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound),
                [StatusCodes.Status409Conflict.ToString(CultureInfo.InvariantCulture)] = ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict),
                [StatusCodes.Status429TooManyRequests.ToString(CultureInfo.InvariantCulture)] = ReasonPhrases.GetReasonPhrase(StatusCodes.Status429TooManyRequests),
                [StatusCodes.Status500InternalServerError.ToString(CultureInfo.InvariantCulture)] = ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)
            };
            
            foreach (var status in statusCodesToAdd.Keys)
            {
                operation.Responses[status] = new OpenApiResponse
                {
                    Description = statusCodesToAdd[status],
                    Content = {
                        [MediaTypes.ProblemJson] = new OpenApiMediaType
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
