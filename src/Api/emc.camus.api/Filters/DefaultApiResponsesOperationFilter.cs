using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using emc.camus.api.Configurations;
using emc.camus.application.Common;

namespace emc.camus.api.Filters
{
    /// <summary>
    /// Operation filter that adds default error responses using ProblemDetails.
    /// Adds 500 to all endpoints and 401/403 only to endpoints that require authentication.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Swagger operation filter tightly coupled to Swashbuckle pipeline; tested indirectly via integration tests")]
    internal sealed class DefaultApiResponsesOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the operation filter to add default API responses.
        /// Adds 500 (Internal Server Error) to all endpoints, and 401/403 only to
        /// endpoints decorated with <see cref="AuthorizeAttribute"/> that are not
        /// overridden by <see cref="AllowAnonymousAttribute"/>.
        /// </summary>
        /// <param name="operation">The OpenAPI operation to modify.</param>
        /// <param name="context">The operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ArgumentNullException.ThrowIfNull(context);

            var schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);

            AddInternalServerErrorResponse(operation, schema);

            if (RequiresAuthentication(context))
            {
                AddUnauthorizedResponse(operation, schema);
                AddForbiddenResponse(operation, schema);
            }
        }

        private static bool RequiresAuthentication(OperationFilterContext context)
        {
            var methodAttributes = context.MethodInfo.GetCustomAttributes(true);
            var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? [];

            bool hasAllowAnonymous = methodAttributes.OfType<AllowAnonymousAttribute>().Any();
            if (hasAllowAnonymous)
            {
                return false;
            }

            bool hasAuthorize = methodAttributes.OfType<AuthorizeAttribute>().Any()
                || controllerAttributes.OfType<AuthorizeAttribute>().Any();

            return hasAuthorize;
        }

        private static void AddInternalServerErrorResponse(OpenApiOperation operation, OpenApiSchema schema)
        {
            var statusCode = StatusCodes.Status500InternalServerError.ToString(CultureInfo.InvariantCulture);

            operation.Responses[statusCode] = new OpenApiResponse
            {
                Description = ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError),
                Content =
                {
                    [MediaTypes.ProblemJson] = new OpenApiMediaType
                    {
                        Schema = schema,
                        Example = new OpenApiObject
                        {
                            ["status"] = new OpenApiInteger(StatusCodes.Status500InternalServerError),
                            ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)),
                            ["detail"] = new OpenApiString("An unexpected error occurred on the server."),
                            ["error"] = new OpenApiString(ErrorCodes.InternalServerError)
                        }
                    }
                }
            };
        }

        private static void AddUnauthorizedResponse(OpenApiOperation operation, OpenApiSchema schema)
        {
            var statusCode = StatusCodes.Status401Unauthorized.ToString(CultureInfo.InvariantCulture);

            operation.Responses[statusCode] = new OpenApiResponse
            {
                Description = ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized),
                Content =
                {
                    [MediaTypes.ProblemJson] = new OpenApiMediaType
                    {
                        Schema = schema,
                        Example = new OpenApiObject
                        {
                            ["status"] = new OpenApiInteger(StatusCodes.Status401Unauthorized),
                            ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized)),
                            ["detail"] = new OpenApiString("You are not authorized to access this resource."),
                            ["error"] = new OpenApiString(ErrorCodes.Unauthorized)
                        }
                    }
                }
            };
        }

        private static void AddForbiddenResponse(OpenApiOperation operation, OpenApiSchema schema)
        {
            var statusCode = StatusCodes.Status403Forbidden.ToString(CultureInfo.InvariantCulture);

            operation.Responses[statusCode] = new OpenApiResponse
            {
                Description = ReasonPhrases.GetReasonPhrase(StatusCodes.Status403Forbidden),
                Content =
                {
                    [MediaTypes.ProblemJson] = new OpenApiMediaType
                    {
                        Schema = schema,
                        Example = new OpenApiObject
                        {
                            ["status"] = new OpenApiInteger(StatusCodes.Status403Forbidden),
                            ["title"] = new OpenApiString(ReasonPhrases.GetReasonPhrase(StatusCodes.Status403Forbidden)),
                            ["detail"] = new OpenApiString("You do not have permission to access this resource."),
                            ["error"] = new OpenApiString(ErrorCodes.Forbidden)
                        }
                    }
                }
            };
        }
    }
}
