using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using emc.camus.api.Controllers;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V1;
using emc.camus.api.test.Helpers;
using emc.camus.application.ApiInfo;
using emc.camus.application.Observability;

namespace emc.camus.api.test.Controllers;

public class ApiInfoControllerTests
{
    private static readonly List<string> FeaturesAuthVersioning = ["auth", "versioning"];
    private static readonly List<string> FeaturesAuth = ["auth"];

    private readonly FakeTimeProvider _timeProvider;
    private readonly FakeActivitySourceWrapper _activitySource;
    private readonly Mock<IApiInfoService> _mockApiInfoService;
    private readonly ApiInfoController _controller;

    public ApiInfoControllerTests()
    {
        _timeProvider = new FakeTimeProvider();
        _activitySource = new FakeActivitySourceWrapper();
        _mockApiInfoService = new Mock<IApiInfoService>();

        _controller = new ApiInfoController(_timeProvider, _activitySource, _mockApiInfoService.Object);

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

    public static readonly TheoryData<TimeProvider?, IActivitySourceWrapper?, IApiInfoService?> Constructor_NullDependencyScenarios = new()
    {
        { null, new FakeActivitySourceWrapper(), new Mock<IApiInfoService>().Object },
        { new FakeTimeProvider(), null, new Mock<IApiInfoService>().Object },
        { new FakeTimeProvider(), new FakeActivitySourceWrapper(), null }
    };

    [Theory]
    [MemberData(nameof(Constructor_NullDependencyScenarios))]
    public void Constructor_NullDependency_ThrowsArgumentNullException(
        TimeProvider? timeProvider, IActivitySourceWrapper? activitySource, IApiInfoService? apiInfoService)
    {
        // Arrange
        // Act
        var act = () => new ApiInfoController(timeProvider!, activitySource!, apiInfoService!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- GetInfo ---

    [Fact]
    public async Task GetInfo_ValidRequest_ReturnsOkWithApiInfoResponseAndSetsActivityTags()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("2.0", "Active", FeaturesAuthVersioning);
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

        _activitySource.RequestTagsCalls.Should().ContainSingle();
        _activitySource.ResponseTagsCalls.Should().ContainSingle();
    }

    [Fact]
    public async Task GetInfo_NullApiVersion_UsesUnknownFallbackInRequestTags()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("1.0", "Active", FeaturesAuth);
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
        _activitySource.RequestTagsCalls.Should().ContainSingle()
            .Which.Should().ContainKey("api_version")
            .WhoseValue.Should().Be("unknown");
    }

    // --- GetInfoApiKey ---

    [Fact]
    public async Task GetInfoApiKey_ValidRequest_ReturnsOkWithApiInfoResponse()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("2.0", "Active", FeaturesAuthVersioning);
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
        var detailView = new ApiInfoDetailView("2.0", "Active", FeaturesAuth);
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
        _activitySource.RequestTagsCalls.Should().ContainSingle()
            .Which.Should().ContainKey("api_version")
            .WhoseValue.Should().Be("unknown");
    }

    // --- GetInfoJwt ---

    [Fact]
    public async Task GetInfoJwt_ValidRequest_ReturnsOkWithApiInfoResponse()
    {
        // Arrange
        var detailView = new ApiInfoDetailView("2.0", "Active", FeaturesAuthVersioning);
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
        var detailView = new ApiInfoDetailView("2.0", "Active", FeaturesAuth);
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
        _activitySource.RequestTagsCalls.Should().ContainSingle()
            .Which.Should().ContainKey("api_version")
            .WhoseValue.Should().Be("unknown");
    }
}
