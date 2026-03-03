using emc.camus.api.Controllers;
using emc.camus.application.ApiInfo;
using emc.camus.application.Auth;
using emc.camus.application.Observability;
using emc.camus.application.RateLimiting;
using emc.camus.api.Models.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace emc.camus.api.test.Controllers;

/// <summary>
/// Unit tests for ApiInfoController to verify API information endpoints.
/// </summary>
public class ApiInfoControllerTests
{
    private readonly Mock<ILogger<ApiInfoController>> _mockLogger;
    private readonly Mock<IActivitySourceWrapper> _mockActivitySource;
    private readonly Mock<ApiInfoService> _mockApiInfoService;

    public ApiInfoControllerTests()
    {
        _mockLogger = new Mock<ILogger<ApiInfoController>>();
        _mockActivitySource = new Mock<IActivitySourceWrapper>();
        
        // Mock ApiInfoService (methods must be virtual)
        var mockApiInfoRepository = new Mock<IApiInfoRepository>();
        _mockApiInfoService = new Mock<ApiInfoService>(mockApiInfoRepository.Object);

        // Setup default activity source behavior
        _mockActivitySource
            .Setup(x => x.StartActivityAndRunAsync<IActionResult>(
                It.IsAny<string>(),
                It.IsAny<OperationType>(),
                It.IsAny<Func<System.Diagnostics.Activity?, Task<IActionResult>>>()))
            .Returns<string, OperationType, Func<System.Diagnostics.Activity?, Task<IActionResult>>>(
                async (name, type, func) => await func(null));

        // Setup default API info service behavior
        _mockApiInfoService
            .Setup(x => x.GetByVersionAsync(It.IsAny<string>()))
            .ReturnsAsync((string version) => new ApiInfoView(
                version,
                Status: "Available",
                Features: new List<string> { "Authentication", "Authorization", "Observability" }
            ));
    }

    [Fact]
    public async Task GetInfo_ShouldReturnPublicApiInfo()
    {
        // Arrange
        var controller = CreateController();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddApiVersioning();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.GetInfo();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeOfType<ApiResponse<ApiInfoResponse>>();
        
        var response = (ApiResponse<ApiInfoResponse>)okResult.Value!;
        response.Message.Should().Be("API information retrieved successfully");
        response.Data.Should().NotBeNull();
        response.Data!.Version.Should().NotBeNullOrEmpty();
        response.Data.Features.Should().Contain("Authentication");
        response.Data.Features.Should().Contain("Authorization");
        response.Data.Features.Should().Contain("Observability");
    }

    [Fact]
    public async Task GetInfoApiKey_ShouldReturnApiKeyProtectedInfo()
    {
        // Arrange
        var controller = CreateController();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddApiVersioning();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.GetInfoApiKey();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeOfType<ApiResponse<ApiInfoResponse>>();
        
        var response = (ApiResponse<ApiInfoResponse>)okResult.Value!;
        response.Data.Should().NotBeNull();
        response.Data!.Status.Should().NotBeNullOrEmpty();
        response.Data!.Version.Should().NotBeNullOrEmpty();
        response.Data!.Features.Should().Contain("Authentication");
        response.Data!.Features.Should().Contain("Authorization");
    }

    [Fact]
    public async Task GetInfoJwt_ShouldReturnJwtProtectedInfo()
    {
        // Arrange
        var controller = CreateController();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddApiVersioning();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.GetInfoJwt();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeOfType<ApiResponse<ApiInfoResponse>>();
        
        var response = (ApiResponse<ApiInfoResponse>)okResult.Value!;
        response.Data.Should().NotBeNull();
        response.Data!.Status.Should().NotBeNullOrEmpty();
        response.Data!.Version.Should().NotBeNullOrEmpty();
        response.Data!.Features.Should().Contain("Authentication");
        response.Data!.Features.Should().Contain("Authorization");
    }

    [Fact]
    public void GetInfo_ShouldOverrideWithRelaxedRateLimit()
    {
        // Arrange & Act
        var methodInfo = typeof(ApiInfoController).GetMethod(nameof(ApiInfoController.GetInfo));
        var rateLimitAttribute = methodInfo!.GetCustomAttributes(typeof(RateLimitAttribute), false)
            .Cast<RateLimitAttribute>()
            .FirstOrDefault();

        // Assert
        rateLimitAttribute.Should().NotBeNull("GetInfo should override with relaxed rate limit");
        rateLimitAttribute!.PolicyName.Should().Be(RateLimitPolicies.Relaxed, 
            "Public info endpoint should use relaxed rate limiting");
    }

    [Fact]
    public void GetInfoApiKey_ShouldHaveApiKeyAuthorizationAttribute()
    {
        // Arrange & Act
        var methodInfo = typeof(ApiInfoController).GetMethod(nameof(ApiInfoController.GetInfoApiKey));
        var authorizeAttribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        // Assert
        authorizeAttribute.Should().NotBeNull("GetInfoApiKey should have Authorize attribute");
        authorizeAttribute!.AuthenticationSchemes.Should().Be(AuthenticationSchemes.ApiKey,
            "Should require API Key authentication");
    }

    [Fact]
    public void GetInfoJwt_ShouldHaveJwtAuthorizationAttribute()
    {
        // Arrange & Act
        var methodInfo = typeof(ApiInfoController).GetMethod(nameof(ApiInfoController.GetInfoJwt));
        var authorizeAttribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        // Assert
        authorizeAttribute.Should().NotBeNull("GetInfoJwt should have Authorize attribute");
        authorizeAttribute!.AuthenticationSchemes.Should().Be(AuthenticationSchemes.JwtBearer,
            "Should require JWT authentication");
    }

    private ApiInfoController CreateController()
    {
        return new ApiInfoController(
            _mockLogger.Object,
            _mockActivitySource.Object,
            _mockApiInfoService.Object);
    }
}
