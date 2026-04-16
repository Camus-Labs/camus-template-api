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
    private readonly Mock<IApiInfoService> _mockApiInfoService;
    private readonly ApiInfoController _controller;

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

        _mockApiInfoService = new Mock<IApiInfoService>();

        _controller = new ApiInfoController(_mockActivitySource.Object, _mockApiInfoService.Object);

        var httpContext = new DefaultHttpContext();
        var mockVersionFeature = new Mock<IApiVersioningFeature>();
        mockVersionFeature.Setup(f => f.RequestedApiVersion).Returns(new ApiVersion(2, 0));
        httpContext.Features.Set(mockVersionFeature.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    // --- Constructor ---

    public static IEnumerable<object?[]> Constructor_NullDependencyScenarios()
    {
        var activitySource = new Mock<IActivitySourceWrapper>().Object;
        var service = new Mock<IApiInfoService>().Object;

        yield return new object?[] { null, service };
        yield return new object?[] { activitySource, null };
    }

    [Theory]
    [MemberData(nameof(Constructor_NullDependencyScenarios))]
    public void Constructor_NullDependency_ThrowsArgumentNullException(
        IActivitySourceWrapper? activitySource, IApiInfoService? apiInfoService)
    {
        // Arrange
        // Act
        var act = () => new ApiInfoController(activitySource!, apiInfoService!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- GetInfo ---

    [Fact]
    public async Task GetInfo_ValidRequest_ReturnsOkWithApiInfoResponseAndSetsActivityTags()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("2.0", "Active", new List<string> { "auth", "versioning" });
        _mockApiInfoService
            .Setup(s => s.GetByVersionAsync(It.IsAny<ApiInfoFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailView);

        // Act
        var result = await _controller.GetInfo(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ApiInfoResponse>>().Subject;
        apiResponse.Data!.Version.Should().Be(detailView.Version);
        apiResponse.Data.Status.Should().Be(detailView.Status);
        apiResponse.Data.Features.Should().BeEquivalentTo(detailView.Features);
        apiResponse.Message.Should().Contain("API information retrieved successfully");

        _mockActivitySource.Verify(
            a => a.SetRequestTags(It.IsAny<Activity?>(), It.IsAny<IDictionary<string, object?>>()),
            Times.Once);
        _mockActivitySource.Verify(
            a => a.SetResponseTags(It.IsAny<Activity?>(), It.IsAny<IDictionary<string, object?>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetInfo_NullApiVersion_UsesUnknownFallbackInRequestTags()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("1.0", "Active", new List<string> { "auth" });
        _mockApiInfoService
            .Setup(s => s.GetByVersionAsync(It.IsAny<ApiInfoFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailView);

        var httpContext = new DefaultHttpContext();
        var mockVersionFeature = new Mock<IApiVersioningFeature>();
        mockVersionFeature.Setup(f => f.RequestedApiVersion).Returns(default(ApiVersion?));
        httpContext.Features.Set(mockVersionFeature.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.GetInfo(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockActivitySource.Verify(
            a => a.SetRequestTags(It.IsAny<Activity?>(), It.Is<IDictionary<string, object?>>(
                d => (string)d["api_version"]! == "unknown")),
            Times.Once);
    }

    // --- GetInfoApiKey ---

    [Fact]
    public async Task GetInfoApiKey_ValidRequest_ReturnsOkWithApiInfoResponse()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("2.0", "Active", new List<string> { "auth", "versioning" });
        _mockApiInfoService
            .Setup(s => s.GetByVersionAsync(It.IsAny<ApiInfoFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailView);

        // Act
        var result = await _controller.GetInfoApiKey(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ApiInfoResponse>>().Subject;
        apiResponse.Data!.Version.Should().Be(detailView.Version);
    }

    [Fact]
    public async Task GetInfoApiKey_NullApiVersion_UsesUnknownFallbackInRequestTags()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("2.0", "Active", new List<string> { "auth" });
        _mockApiInfoService
            .Setup(s => s.GetByVersionAsync(It.IsAny<ApiInfoFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailView);

        var httpContext = new DefaultHttpContext();
        var mockVersionFeature = new Mock<IApiVersioningFeature>();
        mockVersionFeature.Setup(f => f.RequestedApiVersion).Returns(default(ApiVersion?));
        httpContext.Features.Set(mockVersionFeature.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.GetInfoApiKey(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockActivitySource.Verify(
            a => a.SetRequestTags(It.IsAny<Activity?>(), It.Is<IDictionary<string, object?>>(
                d => (string)d["api_version"]! == "unknown")),
            Times.Once);
    }

    // --- GetInfoJwt ---

    [Fact]
    public async Task GetInfoJwt_ValidRequest_ReturnsOkWithApiInfoResponse()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("2.0", "Active", new List<string> { "auth", "versioning" });
        _mockApiInfoService
            .Setup(s => s.GetByVersionAsync(It.IsAny<ApiInfoFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailView);

        // Act
        var result = await _controller.GetInfoJwt(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ApiInfoResponse>>().Subject;
        apiResponse.Data!.Version.Should().Be(detailView.Version);
    }

    [Fact]
    public async Task GetInfoJwt_NullApiVersion_UsesUnknownFallbackInRequestTags()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("2.0", "Active", new List<string> { "auth" });
        _mockApiInfoService
            .Setup(s => s.GetByVersionAsync(It.IsAny<ApiInfoFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailView);

        var httpContext = new DefaultHttpContext();
        var mockVersionFeature = new Mock<IApiVersioningFeature>();
        mockVersionFeature.Setup(f => f.RequestedApiVersion).Returns(default(ApiVersion?));
        httpContext.Features.Set(mockVersionFeature.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.GetInfoJwt(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockActivitySource.Verify(
            a => a.SetRequestTags(It.IsAny<Activity?>(), It.Is<IDictionary<string, object?>>(
                d => (string)d["api_version"]! == "unknown")),
            Times.Once);
    }
}
