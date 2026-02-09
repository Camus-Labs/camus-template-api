using System.Security.Claims;
using System.Text.Encodings.Web;
using emc.camus.application.Auth;
using emc.camus.application.Secrets;
using emc.camus.security.apikey.Configurations;
using emc.camus.security.apikey.Handlers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace emc.camus.security.apikey.test.Handlers;

/// <summary>
/// Unit tests for ApiKeyAuthenticationHandler to verify API Key authentication logic.
/// </summary>
public class ApiKeyAuthenticationHandlerTests
{
    private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _mockOptions;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<ApiKeyAuthenticationHandler>> _mockLogger;
    private readonly Mock<UrlEncoder> _mockUrlEncoder;
    private readonly Mock<ISecretProvider> _mockSecretProvider;
    private readonly ApiKeySettings _settings;
    private readonly AuthenticationScheme _scheme;

    public ApiKeyAuthenticationHandlerTests()
    {
        _mockOptions = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<ApiKeyAuthenticationHandler>>();
        _mockUrlEncoder = new Mock<UrlEncoder>();
        _mockSecretProvider = new Mock<ISecretProvider>();
        
        _settings = new ApiKeySettings
        {
            SecretKeyName = "XApiKey"
        };

        _scheme = new AuthenticationScheme(
            AuthenticationSchemes.ApiKey,
            AuthenticationSchemes.ApiKey,
            typeof(ApiKeyAuthenticationHandler));

        _mockOptions.Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
        
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithValidApiKey_ShouldSucceed()
    {
        // Arrange
        var validApiKey = "valid-api-key-123";
        _mockSecretProvider.Setup(x => x.GetSecret("XApiKey")).Returns(validApiKey);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = validApiKey;

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Principal!.Identity.Should().NotBeNull();
        result.Principal.Identity!.IsAuthenticated.Should().BeTrue();
        result.Principal.Identity.AuthenticationType.Should().Be(AuthenticationSchemes.ApiKey);
        
        var claim = result.Principal.FindFirst(ClaimTypes.Name);
        claim.Should().NotBeNull();
        claim!.Value.Should().Be("ApiKeyUser");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithMissingHeader_ShouldFail()
    {
        // Arrange
        var context = new DefaultHttpContext();
        // No X-Api-Key header set

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*API Key*");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithEmptyApiKey_ShouldFail()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = string.Empty;

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid API Key*");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithInvalidApiKey_ShouldFail()
    {
        // Arrange
        var validApiKey = "valid-api-key-123";
        var invalidApiKey = "invalid-api-key";
        
        _mockSecretProvider.Setup(x => x.GetSecret("XApiKey")).Returns(validApiKey);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = invalidApiKey;

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid API Key*");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithWhitespaceApiKey_ShouldFail()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "   ";

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid API Key*");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WhenSecretProviderReturnsNull_ShouldFail()
    {
        // Arrange
        _mockSecretProvider.Setup(x => x.GetSecret("XApiKey")).Returns((string?)null);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "some-api-key";

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid API Key*");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WhenSecretProviderReturnsEmpty_ShouldFail()
    {
        // Arrange
        _mockSecretProvider.Setup(x => x.GetSecret("XApiKey")).Returns(string.Empty);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "some-api-key";

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid API Key*");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldCallSecretProvider()
    {
        // Arrange
        var validApiKey = "valid-api-key-123";
        _mockSecretProvider.Setup(x => x.GetSecret("XApiKey")).Returns(validApiKey);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = validApiKey;

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act
        await handler.AuthenticateAsync();

        // Assert
        _mockSecretProvider.Verify(x => x.GetSecret("XApiKey"), Times.Once);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithValidApiKey_ShouldCreateCorrectClaims()
    {
        // Arrange
        var validApiKey = "valid-api-key-123";
        _mockSecretProvider.Setup(x => x.GetSecret("XApiKey")).Returns(validApiKey);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = validApiKey;

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        
        var claims = result.Principal!.Claims.ToList();
        claims.Should().HaveCount(1);
        claims.First().Type.Should().Be(ClaimTypes.Name);
        claims.First().Value.Should().Be("ApiKeyUser");
    }

    [Theory]
    [InlineData("key-123")]
    [InlineData("super-secret-key")]
    [InlineData("abcd1234efgh5678")]
    public async Task HandleAuthenticateAsync_WithVariousValidKeys_ShouldSucceed(string apiKey)
    {
        // Arrange
        _mockSecretProvider.Setup(x => x.GetSecret("XApiKey")).Returns(apiKey);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = apiKey;

        var handler = CreateHandler();
        await InitializeHandler(handler, context, _scheme);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    private ApiKeyAuthenticationHandler CreateHandler()
    {
        return new ApiKeyAuthenticationHandler(
            _mockOptions.Object,
            _mockLoggerFactory.Object,
            _mockUrlEncoder.Object,
            _mockSecretProvider.Object,
            _settings);
    }

    private async Task InitializeHandler(
        AuthenticationHandler<AuthenticationSchemeOptions> handler,
        HttpContext context,
        AuthenticationScheme scheme)
    {
        await handler.InitializeAsync(scheme, context);
    }
}
