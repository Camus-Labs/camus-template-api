using System.Security;
using System.Security.Claims;
using emc.camus.api.Controllers;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Observability;
using emc.camus.application.RateLimiting;
using emc.camus.application.Secrets;
using emc.camus.domain.Auth;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Requests;
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
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly Mock<IActivitySourceWrapper> _mockActivitySource;
    private readonly Mock<AuthService> _mockAuthService;

    public AuthControllerTests()
    {
        _mockLogger = new Mock<ILogger<AuthController>>();
        _mockActivitySource = new Mock<IActivitySourceWrapper>();
        
        // Mock concrete AuthService (methods must be virtual)
        var mockUserRepository = new Mock<IUserRepository>();
        var mockTokenGenerator = new Mock<ITokenGenerator>();
        var mockConnectionFactory = new Mock<IConnectionFactory>();
        var mockAuditRepository = new Mock<IActionAuditRepository>();
        _mockAuthService = new Mock<AuthService>(
            mockUserRepository.Object, 
            mockTokenGenerator.Object,
            mockConnectionFactory.Object,
            mockAuditRepository.Object);

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
    public async Task AuthenticateUser_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var controller = CreateController();
        var credentials = new AuthenticateUserRequest
        {
            Username = "testuser",
            Password = "testpassword"
        };

        var expectedResult = new AuthenticateUserResult(
            "fake-jwt-token",
            DateTime.UtcNow.AddHours(2)
        );
        _mockAuthService
            .Setup(x => x.AuthenticateAsync(It.IsAny<AuthenticateUserCommand>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.AuthenticateUser(credentials);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeOfType<ApiResponse<AuthenticateUserResponse>>();
        
        var response = (ApiResponse<AuthenticateUserResponse>)okResult.Value!;
        response.Message.Should().Be("User authenticated successfully");
        response.Data.Should().NotBeNull();
        response.Data!.Token.Should().Be("fake-jwt-token");
        response.Data.ExpiresOn.Should().BeCloseTo(expectedResult.ExpiresOn, TimeSpan.FromSeconds(1));

        _mockAuthService.Verify(x => x.AuthenticateAsync(
            It.Is<AuthenticateUserCommand>(c => c.Username == "testuser" && c.Password == "testpassword")),
            Times.Once);
    }

    [Theory]
    [InlineData(null, "testpassword")] // Null Username
    [InlineData("", "testpassword")] // Empty Username
    [InlineData("   ", "testpassword")] // Whitespace Username
    [InlineData("testuser", null)] // Null Password
    [InlineData("testuser", "")] // Empty Password
    [InlineData("testuser", "   ")] // Whitespace Password
    public async Task AuthenticateUser_WithInvalidInput_ShouldThrowArgumentException(
        string? username, string? password)
    {
        // Arrange
        var controller = CreateController();
        var credentials = new AuthenticateUserRequest
        {
            Username = username ?? string.Empty,
            Password = password ?? string.Empty
        };

        // Act & Assert - Validation happens in mapper extension before reaching service
        var act = async () => await controller.AuthenticateUser(credentials);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*required*");
    }

    [Theory]
    [InlineData("wronguser", "testpassword")] // Wrong Username
    [InlineData("testuser", "wrongpassword")] // Wrong Password
    public async Task AuthenticateUser_WithInvalidCredentials_ShouldThrowUnauthorized(
        string username, string password)
    {
        // Arrange
        var controller = CreateController();
        var credentials = new AuthenticateUserRequest
        {
            Username = username,
            Password = password
        };

        _mockAuthService
            .Setup(x => x.AuthenticateAsync(It.IsAny<AuthenticateUserCommand>()))
            .ThrowsAsync(new UnauthorizedAccessException("The provided credentials are invalid"));

        // Act & Assert
        var act = async () => await controller.AuthenticateUser(credentials);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*The provided credentials are invalid*");
    }

    [Fact]
    public async Task AuthenticateUser_WhenAuthServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var controller = CreateController();
        var credentials = new AuthenticateUserRequest
        {
            Username = "testuser",
            Password = "testpassword"
        };

        _mockAuthService
            .Setup(x => x.AuthenticateAsync(It.IsAny<AuthenticateUserCommand>()))
            .ThrowsAsync(new InvalidOperationException("Secret not found"));

        // Act & Assert
        var act = async () => await controller.AuthenticateUser(credentials);
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
    public void AuthenticateUser_ShouldInheritStrictRateLimitFromController()
    {
        // Arrange & Act
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.AuthenticateUser));
        var methodAttributes = methodInfo!.GetCustomAttributes(typeof(RateLimitAttribute), true);
        
        var controllerType = typeof(AuthController);
        var controllerAttributes = controllerType.GetCustomAttributes(typeof(RateLimitAttribute), false);

        // Assert
        // Method doesn't override, so it inherits from controller
        methodAttributes.Should().BeEmpty("AuthenticateUser method should inherit rate limit from controller");
        controllerAttributes.Should().NotBeEmpty("AuthController should have RateLimit attribute");
        
        var controllerRateLimit = controllerAttributes.Cast<RateLimitAttribute>().First();
        controllerRateLimit.PolicyName.Should().Be(RateLimitPolicies.Strict);
    }

    private AuthController CreateController()
    {
        return new AuthController(
            _mockLogger.Object,
            _mockActivitySource.Object,
            _mockAuthService.Object);
    }
}
