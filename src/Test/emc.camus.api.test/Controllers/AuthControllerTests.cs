using System.Security;
using System.Security.Claims;
using emc.camus.api.Controllers;
using emc.camus.application.Auth;
using emc.camus.application.Observability;
using emc.camus.application.RateLimiting;
using emc.camus.application.Secrets;
using emc.camus.domain.Auth;
using emc.camus.domain.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace emc.camus.api.test.Controllers;

/// <summary>
/// Unit tests for AuthController to verify authentication endpoints and business logic.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ISecretProvider> _mockSecretProvider;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly Mock<IActivitySourceWrapper> _mockActivitySource;
    private readonly Mock<IJwtTokenGenerator> _mockTokenGenerator;

    public AuthControllerTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockSecretProvider = new Mock<ISecretProvider>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _mockActivitySource = new Mock<IActivitySourceWrapper>();
        _mockTokenGenerator = new Mock<IJwtTokenGenerator>();

        // Setup default activity source behavior
        _mockActivitySource
            .Setup(x => x.StartActivityAndRunAsync<IActionResult>(
                It.IsAny<string>(),
                It.IsAny<OperationType>(),
                It.IsAny<Func<System.Diagnostics.Activity?, Task<IActionResult>>>()))
            .Returns<string, OperationType, Func<System.Diagnostics.Activity?, Task<IActionResult>>>(
                async (name, type, func) => await func(null));
    }

    [Fact]
    public async Task GetInfoV1_ShouldReturnPublicApiInfo()
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
        var result = await controller.GetInfoV1();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeOfType<ApiResponse<ApiInfo>>();
        
        var response = (ApiResponse<ApiInfo>)okResult.Value!;
        response.Message.Should().Be("API information retrieved successfully");
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("My Basic API");
        response.Data.Features.Should().Contain("Authentication");
        response.Data.Features.Should().Contain("Authorization");
        response.Data.Features.Should().Contain("Observability");
    }

    [Fact]
    public async Task GetInfoV2ApiKey_ShouldReturnApiKeyProtectedInfo()
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
        var result = await controller.GetInfoV2ApiKey();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeOfType<ApiResponse<ApiInfo>>();
        
        var response = (ApiResponse<ApiInfo>)okResult.Value!;
        response.Data.Should().NotBeNull();
        response.Data!.Status.Should().Contain("API Key");
    }

    [Fact]
    public async Task GetInfoV2Jwt_ShouldReturnJwtProtectedInfo()
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
        var result = await controller.GetInfoV2Jwt();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeOfType<ApiResponse<ApiInfo>>();
        
        var response = (ApiResponse<ApiInfo>)okResult.Value!;
        response.Data.Should().NotBeNull();
        response.Data!.Status.Should().Contain("JWT");
    }

    [Fact]
    public async Task GenerateToken_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var controller = CreateController();
        var credentials = new Credentials
        {
            AccessKey = "testkey",
            AccessSecret = "testsecret"
        };

        _mockSecretProvider.Setup(x => x.GetSecret("AccessKey")).Returns("testkey");
        _mockSecretProvider.Setup(x => x.GetSecret("AccessSecret")).Returns("testsecret");

        var expectedTokenResult = new JwtTokenResult
        {
            Token = "fake-jwt-token",
            ExpiresOn = DateTime.UtcNow.AddHours(2)
        };
        _mockTokenGenerator
            .Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<IEnumerable<Claim>>()))
            .Returns(expectedTokenResult);

        // Act
        var result = await controller.GenerateToken(credentials);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeOfType<ApiResponse<AuthToken>>();
        
        var response = (ApiResponse<AuthToken>)okResult.Value!;
        response.Message.Should().Be("Token generated successfully");
        response.Data.Should().NotBeNull();
        response.Data!.Token.Should().Be("fake-jwt-token");
        response.Data.ExpiresOn.Should().BeCloseTo(expectedTokenResult.ExpiresOn, TimeSpan.FromSeconds(1));

        _mockTokenGenerator.Verify(x => x.GenerateToken(
            "testkey",
            It.Is<IEnumerable<Claim>>(claims => 
                claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "User") &&
                claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "ApiClient"))),
            Times.Once);
    }

    [Theory]
    [InlineData(null, "testsecret", "testkey", "testsecret")] // Null AccessKey
    [InlineData("", "testsecret", "testkey", "testsecret")] // Empty AccessKey
    [InlineData("   ", "testsecret", "testkey", "testsecret")] // Whitespace AccessKey
    [InlineData("wrongkey", "testsecret", "testkey", "testsecret")] // Wrong AccessKey
    [InlineData("testkey", null, "testkey", "testsecret")] // Null AccessSecret
    [InlineData("testkey", "", "testkey", "testsecret")] // Empty AccessSecret
    [InlineData("testkey", "   ", "testkey", "testsecret")] // Whitespace AccessSecret
    [InlineData("testkey", "wrongsecret", "testkey", "testsecret")] // Wrong AccessSecret
    public async Task GenerateToken_WithInvalidCredentials_ShouldThrowUnauthorized(
        string? accessKey, string? accessSecret, string correctKey, string correctSecret)
    {
        // Arrange
        var controller = CreateController();
        var credentials = new Credentials
        {
            AccessKey = accessKey,
            AccessSecret = accessSecret
        };

        _mockSecretProvider.Setup(x => x.GetSecret("AccessKey")).Returns(correctKey);
        _mockSecretProvider.Setup(x => x.GetSecret("AccessSecret")).Returns(correctSecret);

        // Act & Assert
        var act = async () => await controller.GenerateToken(credentials);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*The provided credentials are invalid*");
    }

    [Fact]
    public async Task GenerateToken_WhenSecretProviderThrows_ShouldPropagateException()
    {
        // Arrange
        var controller = CreateController();
        var credentials = new Credentials
        {
            AccessKey = "testkey",
            AccessSecret = "testsecret"
        };

        _mockSecretProvider.Setup(x => x.GetSecret(It.IsAny<string>()))
            .Throws<SecurityException>();

        // Act & Assert
        var act = async () => await controller.GenerateToken(credentials);
        await act.Should().ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task GenerateToken_WhenTokenGeneratorThrows_ShouldPropagateException()
    {
        // Arrange
        var controller = CreateController();
        var credentials = new Credentials
        {
            AccessKey = "testkey",
            AccessSecret = "testsecret"
        };

        _mockSecretProvider.Setup(x => x.GetSecret("AccessKey")).Returns("testkey");
        _mockSecretProvider.Setup(x => x.GetSecret("AccessSecret")).Returns("testsecret");
        _mockTokenGenerator
            .Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<IEnumerable<Claim>>()))
            .Throws<InvalidOperationException>();

        // Act & Assert
        var act = async () => await controller.GenerateToken(credentials);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetUnexpectedError_ShouldThrowException()
    {
        // Arrange
        var controller = CreateController();

        // Act & Assert
        var act = async () => await controller.GetUnexpectedError();
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("This is a demo exception for error handling.");
    }

    [Fact]
    public void AuthController_ShouldHaveStrictRateLimitAttribute()
    {
        // Arrange & Act
        var controllerType = typeof(AuthController);
        var rateLimitAttribute = controllerType.GetCustomAttributes(typeof(RateLimitAttribute), false)
            .Cast<RateLimitAttribute>()
            .FirstOrDefault();

        // Assert
        rateLimitAttribute.Should().NotBeNull("AuthController should have RateLimit attribute to protect authentication endpoints");
        rateLimitAttribute!.PolicyName.Should().Be(RateLimitPolicies.Strict, 
            "AuthController should use strict rate limiting policy to prevent brute force attacks");
    }

    [Fact]
    public void GenerateToken_ShouldInheritStrictRateLimitFromController()
    {
        // Arrange & Act
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.GenerateToken));
        var methodAttributes = methodInfo!.GetCustomAttributes(typeof(RateLimitAttribute), true);
        
        var controllerType = typeof(AuthController);
        var controllerAttributes = controllerType.GetCustomAttributes(typeof(RateLimitAttribute), false);

        // Assert
        // Method doesn't override, so it inherits from controller
        methodAttributes.Should().BeEmpty("GenerateToken method should inherit rate limit from controller");
        controllerAttributes.Should().NotBeEmpty("AuthController should have RateLimit attribute");
        
        var controllerRateLimit = controllerAttributes.Cast<RateLimitAttribute>().First();
        controllerRateLimit.PolicyName.Should().Be(RateLimitPolicies.Strict);
    }

    [Fact]
    public void GetInfoV1_ShouldOverrideWithRelaxedRateLimit()
    {
        // Arrange & Act
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.GetInfoV1));
        var rateLimitAttribute = methodInfo!.GetCustomAttributes(typeof(RateLimitAttribute), false)
            .Cast<RateLimitAttribute>()
            .FirstOrDefault();

        // Assert
        rateLimitAttribute.Should().NotBeNull("GetInfoV1 should override controller rate limit");
        rateLimitAttribute!.PolicyName.Should().Be(RateLimitPolicies.Relaxed, 
            "Public info endpoint should use relaxed rate limiting");
    }

    private AuthController CreateController()
    {
        return new AuthController(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockActivitySource.Object,
            _mockSecretProvider.Object,
            _mockTokenGenerator.Object);
    }
}
