using System.Diagnostics;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using emc.camus.api.Controllers;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V1;
using emc.camus.application.ApiInfo;
using emc.camus.application.Observability;

namespace emc.camus.api.test.Controllers;

public class ApiInfoControllerTests
{
    private readonly Mock<IActivitySourceWrapper> _mockActivitySource;
    private readonly Mock<ApiInfoService> _mockApiInfoService;
    private readonly ApiInfoController _controller;

    private static readonly ApiInfoDetailView FixedDetailView = new(
        "2.0",
        "Active",
        new List<string> { "auth", "versioning" });

    public ApiInfoControllerTests()
    {
        _mockActivitySource = new Mock<IActivitySourceWrapper>();
        _mockActivitySource
            .Setup(x => x.StartActivityAndRunAsync<IActionResult>(
                It.IsAny<string>(),
                It.IsAny<OperationType>(),
                It.IsAny<Func<Activity?, Task<IActionResult>>>()))
            .Returns<string, OperationType, Func<Activity?, Task<IActionResult>>>(
                (_, _, func) => func(null));

        var mockRepo = new Mock<IApiInfoRepository>();
        _mockApiInfoService = new Mock<ApiInfoService>(mockRepo.Object);

        _controller = new ApiInfoController(_mockActivitySource.Object, _mockApiInfoService.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IApiVersioningFeature>(Mock.Of<IApiVersioningFeature>());
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullActivitySource_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepo = new Mock<IApiInfoRepository>();
        var service = new Mock<ApiInfoService>(mockRepo.Object);

        // Act
        var act = () => new ApiInfoController(null!, service.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullApiInfoService_ThrowsArgumentNullException()
    {
        // Arrange
        var mockActivitySource = new Mock<IActivitySourceWrapper>();

        // Act
        var act = () => new ApiInfoController(mockActivitySource.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- GetInfo ---

    [Fact]
    public async Task GetInfo_ValidRequest_ReturnsOkWithApiInfoResponseAndSetsActivityTags()
    {
        // Arrange
        _mockApiInfoService
            .Setup(s => s.GetByVersionAsync(It.IsAny<ApiInfoFilter>()))
            .ReturnsAsync(FixedDetailView);

        // Act
        var result = await _controller.GetInfo();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ApiInfoResponse>>().Subject;
        apiResponse.Data!.Version.Should().Be("2.0");
        apiResponse.Data.Status.Should().Be("Active");
        apiResponse.Data.Features.Should().BeEquivalentTo(new List<string> { "auth", "versioning" });
        apiResponse.Message.Should().Contain("API information retrieved successfully");

        _mockActivitySource.Verify(
            a => a.SetRequestTags(It.IsAny<Activity?>(), It.IsAny<IDictionary<string, object?>>()),
            Times.Once);
        _mockActivitySource.Verify(
            a => a.SetResponseTags(It.IsAny<Activity?>(), It.IsAny<IDictionary<string, object?>>()),
            Times.Once);
    }

    // --- GetInfoApiKey ---

    [Fact]
    public async Task GetInfoApiKey_ValidRequest_ReturnsOkWithApiInfoResponse()
    {
        // Arrange
        _mockApiInfoService
            .Setup(s => s.GetByVersionAsync(It.IsAny<ApiInfoFilter>()))
            .ReturnsAsync(FixedDetailView);

        // Act
        var result = await _controller.GetInfoApiKey();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ApiInfoResponse>>().Subject;
        apiResponse.Data!.Version.Should().Be("2.0");
    }

    // --- GetInfoJwt ---

    [Fact]
    public async Task GetInfoJwt_ValidRequest_ReturnsOkWithApiInfoResponse()
    {
        // Arrange
        _mockApiInfoService
            .Setup(s => s.GetByVersionAsync(It.IsAny<ApiInfoFilter>()))
            .ReturnsAsync(FixedDetailView);

        // Act
        var result = await _controller.GetInfoJwt();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ApiInfoResponse>>().Subject;
        apiResponse.Data!.Version.Should().Be("2.0");
    }
}
