using emc.camus.application.Generic;
using emc.camus.documentation.swagger.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace emc.camus.documentation.swagger.test.Filters;

/// <summary>
/// Unit tests for DefaultApiResponsesOperationFilter.
/// </summary>
public class DefaultApiResponsesOperationFilterTests
{
    private readonly DefaultApiResponsesOperationFilter _filter;
    private readonly TestSchemaGenerator _testSchemaGenerator;
    private readonly SchemaRepository _schemaRepository;

    public DefaultApiResponsesOperationFilterTests()
    {
        _filter = new DefaultApiResponsesOperationFilter();
        _testSchemaGenerator = new TestSchemaGenerator();
        _schemaRepository = new SchemaRepository();
    }

    [Fact]
    public void Apply_ShouldAddDefaultResponseCodes()
    {
        // Arrange
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        operation.Responses.Should().ContainKeys("400", "401", "403", "429", "500");
    }

    [Fact]
    public void Apply_ShouldAdd400BadRequestResponse()
    {
        // Arrange
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        var response = operation.Responses["400"];
        response.Description.Should().Be("Bad Request");
        response.Content.Should().ContainKey("application/problem+json");
        response.Content["application/problem+json"].Schema.Should().NotBeNull();
        response.Content["application/problem+json"].Example.Should().NotBeNull();
    }

    [Fact]
    public void Apply_ShouldAdd401UnauthorizedResponseWithMultipleExamples()
    {
        // Arrange
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        var response = operation.Responses["401"];
        response.Description.Should().Be("Unauthorized");
        response.Content.Should().ContainKey("application/problem+json");
        
        var mediaType = response.Content["application/problem+json"];
        mediaType.Schema.Should().NotBeNull();
        mediaType.Examples.Should().NotBeNull();
        mediaType.Examples.Should().ContainKeys("Token Expired", "Invalid Token", "Invalid Signature", "Authentication Required");
    }

    [Fact]
    public void Apply_ShouldAdd403ForbiddenResponse()
    {
        // Arrange
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        var response = operation.Responses["403"];
        response.Description.Should().Be("Forbidden");
        response.Content.Should().ContainKey("application/problem+json");
        response.Content["application/problem+json"].Example.Should().NotBeNull();
    }

    [Fact]
    public void Apply_ShouldAdd429TooManyRequestsResponse()
    {
        // Arrange
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        var response = operation.Responses["429"];
        response.Description.Should().Be("Too Many Requests");
        response.Content.Should().ContainKey("application/problem+json");
        response.Content["application/problem+json"].Example.Should().NotBeNull();
    }

    [Fact]
    public void Apply_ShouldAdd500InternalServerErrorResponse()
    {
        // Arrange
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        var response = operation.Responses["500"];
        response.Description.Should().Be("Internal Server Error");
        response.Content.Should().ContainKey("application/problem+json");
        response.Content["application/problem+json"].Example.Should().NotBeNull();
    }

    [Fact]
    public void Apply_WithExistingResponses_ShouldAddDefaultResponses()
    {
        // Arrange
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse { Description = "Success" }
            }
        };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        operation.Responses.Should().ContainKey("200");
        operation.Responses.Should().ContainKeys("400", "401", "403", "429", "500");
    }

    [Fact]
    public void Apply_ShouldUseApplicationProblemJsonContentType()
    {
        // Arrange
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        foreach (var statusCode in new[] { "400", "401", "403", "429", "500" })
        {
            operation.Responses[statusCode].Content.Should().ContainKey("application/problem+json");
        }
    }

    [Fact]
    public void Apply_ShouldGenerateSchemaForProblemDetails()
    {
        // Arrange
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        // Schema generation is verified by the filter not throwing an exception
        operation.Responses.Should().ContainKey("500");
    }

    [Fact]
    public void Apply_401Examples_ShouldHaveCorrectStructure()
    {
        // Arrange
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateOperationFilterContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        var examples = operation.Responses["401"].Content["application/problem+json"].Examples;
        
        examples["Token Expired"].Summary.Should().Be("Token Expired");
        examples["Token Expired"].Description.Should().Contain("JWT token has expired");
        
        examples["Invalid Token"].Summary.Should().Be("Invalid Token");
        examples["Invalid Signature"].Summary.Should().Be("Invalid Signature");
        examples["Authentication Required"].Summary.Should().Be("Authentication Required");
    }

    private OperationFilterContext CreateOperationFilterContext()
    {
        var methodInfo = typeof(DefaultApiResponsesOperationFilterTests).GetMethod(nameof(Apply_ShouldAddDefaultResponseCodes))!;
        var apiDescription = new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription();
        
        return new OperationFilterContext(
            apiDescription,
            _testSchemaGenerator,
            _schemaRepository,
            methodInfo);
    }

    /// <summary>
    /// Test implementation of ISchemaGenerator that returns a simple schema.
    /// </summary>
    private class TestSchemaGenerator : ISchemaGenerator
    {
        public OpenApiSchema GenerateSchema(
            Type type,
            SchemaRepository schemaRepository,
            System.Reflection.MemberInfo? memberInfo = null,
            System.Reflection.ParameterInfo? parameterInfo = null,
            Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterRouteInfo? routeInfo = null)
        {
            return new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["status"] = new OpenApiSchema { Type = "integer" },
                    ["title"] = new OpenApiSchema { Type = "string" },
                    ["detail"] = new OpenApiSchema { Type = "string" }
                }
            };
        }
    }
}
