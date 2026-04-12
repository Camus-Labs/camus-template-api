using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using emc.camus.api.Controllers;
using emc.camus.api.Models.Responses;

namespace emc.camus.api.test.Controllers;

public class ApiControllerBaseTests
{
    private sealed class TestableController : ApiControllerBase
    {
        public IActionResult CallSuccess<T>(T data, string message) => Success(data, message);
        public IActionResult CallCreated<T>(T data, string message, string? actionName = null, object? routeValues = null)
            => Created(data, message, actionName, routeValues);
        public IActionResult CallAccepted<T>(T data, string message) => Accepted(data, message);
        public IActionResult CallNoContentSuccess() => NoContentSuccess();
    }

    private static TestableController CreateController()
    {
        var controller = new TestableController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    // --- Success ---

    [Fact]
    public void Success_ReturnsOkObjectResultWithApiResponse()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.CallSuccess("test-data", "Operation succeeded");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        response.Data.Should().Be("test-data");
        response.Message.Should().Be("Operation succeeded");
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // --- Created ---

    [Fact]
    public void Created_WithoutLocation_ReturnsCreatedAtActionResultWithApiResponse()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.CallCreated("new-resource", "Resource created");

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);

        var response = createdResult.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        response.Data.Should().Be("new-resource");
        response.Message.Should().Be("Resource created");
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Created_WithActionNameAndRouteValues_SetsLocationParameters()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.CallCreated("data", "Created", "GetById", new { id = 42 });

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be("GetById");
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(42);
    }

    // --- Accepted ---

    [Fact]
    public void Accepted_ReturnsAcceptedResultWithApiResponse()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.CallAccepted("op-123", "Request accepted");

        // Assert
        var acceptedResult = result.Should().BeOfType<AcceptedResult>().Subject;
        acceptedResult.StatusCode.Should().Be(202);

        var response = acceptedResult.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        response.Data.Should().Be("op-123");
        response.Message.Should().Be("Request accepted");
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // --- NoContentSuccess ---

    [Fact]
    public void NoContentSuccess_ReturnsNoContentResult()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.CallNoContentSuccess();

        // Assert
        var noContentResult = result.Should().BeOfType<NoContentResult>().Subject;
        noContentResult.StatusCode.Should().Be(204);
    }
}
