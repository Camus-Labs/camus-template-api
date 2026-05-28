using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using emc.camus.api.Models.Responses;
using emc.camus.api.test.Helpers;

namespace emc.camus.api.test.Controllers;

public class ApiControllerBaseTests
{
    private readonly FakeTimeProvider _timeProvider;

    public ApiControllerBaseTests()
    {
        _timeProvider = new FakeTimeProvider();
    }

    private TestableController CreateController()
    {
        var controller = new TestableController(_timeProvider);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    // --- Success ---

    [Fact]
    public void Success_WhenCalled_ReturnsOkObjectResultWithApiResponse()
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
        response.Timestamp.Should().Be(_timeProvider.GetUtcNow().UtcDateTime);
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
        response.Timestamp.Should().Be(_timeProvider.GetUtcNow().UtcDateTime);
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
    public void Accepted_WhenCalled_ReturnsAcceptedResultWithApiResponse()
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
        response.Timestamp.Should().Be(_timeProvider.GetUtcNow().UtcDateTime);
    }

    // --- NoContentSuccess ---

    [Fact]
    public void NoContentSuccess_WhenCalled_ReturnsNoContentResult()
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
