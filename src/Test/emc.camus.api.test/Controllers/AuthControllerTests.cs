using System.Security.Claims;
using emc.camus.api.Controllers;
using emc.camus.application.Auth;
using emc.camus.application.Observability;
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

    [Fact]
    public async Task GenerateToken_WithInvalidAccessKey_ShouldThrowUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        var credentials = new Credentials
        {
            AccessKey = "wrongkey",
            AccessSecret = "testsecret"
        };

        _mockSecretProvider.Setup(x => x.GetSecret("AccessKey")).Returns("correctkey");
        _mockSecretProvider.Setup(x => x.GetSecret("AccessSecret")).Returns("testsecret");

        // Act & Assert
        var act = async () => await controller.GenerateToken(credentials);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task GenerateToken_WithInvalidAccessSecret_ShouldThrowUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        var credentials = new Credentials
        {
            AccessKey = "testkey",
            AccessSecret = "wrongsecret"
        };

        _mockSecretProvider.Setup(x => x.GetSecret("AccessKey")).Returns("testkey");
        _mockSecretProvider.Setup(x => x.GetSecret("AccessSecret")).Returns("correctsecret");

        // Act & Assert
        var act = async () => await controller.GenerateToken(credentials);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task GenerateToken_WithNullCredentials_ShouldThrowUnauthorized()
    {
        // Arrange
        var controller = CreateController();
        var credentials = new Credentials
        {
            AccessKey = null,
            AccessSecret = null
        };

        _mockSecretProvider.Setup(x => x.GetSecret("AccessKey")).Returns("testkey");
        _mockSecretProvider.Setup(x => x.GetSecret("AccessSecret")).Returns("testsecret");

        // Act & Assert
        var act = async () => await controller.GenerateToken(credentials);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GenerateToken_ShouldCallSecretProviderForCredentials()
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
        await controller.GenerateToken(credentials);

        // Assert
        _mockSecretProvider.Verify(x => x.GetSecret("AccessKey"), Times.Once);
        _mockSecretProvider.Verify(x => x.GetSecret("AccessSecret"), Times.Once);
    }

    [Fact]
    public async Task GenerateToken_ShouldSetActivityTags()
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
        await controller.GenerateToken(credentials);

        // Assert
        _mockActivitySource.Verify(x => x.SetRequestTags(
            It.IsAny<System.Diagnostics.Activity?>(),
            It.Is<Dictionary<string, object?>>(tags => tags.ContainsKey("accessKey"))),
            Times.Once);

        _mockActivitySource.Verify(x => x.SetResponseTags(
            It.IsAny<System.Diagnostics.Activity?>(),
            It.Is<Dictionary<string, object?>>(tags => tags.ContainsKey("expiresOn"))),
            Times.Once);
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
