using System.Security.Claims;
using System.Text.Encodings.Web;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Secrets;
using emc.camus.security.apikey.Configurations;
using emc.camus.security.apikey.Handlers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace emc.camus.security.apikey.test.Handlers;

/// <summary>
/// Comprehensive unit tests for ApiKeyAuthenticationHandler covering authentication logic,
/// error handling, security scenarios, and edge cases.
/// </summary>
public class ApiKeyAuthenticationHandlerTests
{
    private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _mockOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Mock<ISecretProvider> _mockSecretProvider;
    private readonly ApiKeySettings _settings;
    private readonly UrlEncoder _encoder;

    public ApiKeyAuthenticationHandlerTests()
    {
        _mockOptions = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        _loggerFactory = NullLoggerFactory.Instance;
        _mockSecretProvider = new Mock<ISecretProvider>();
        _settings = new ApiKeySettings();
        _encoder = UrlEncoder.Default;

        _mockOptions.Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
    }

    #region Success Scenarios

    [Fact]
    public async Task HandleAuthenticateAsync_WithValidApiKey_ShouldSucceed()
    {
        // Arrange
        var validApiKey = "valid-api-key-123";
        _mockSecretProvider.Setup(x => x.GetSecret(_settings.ApiKeySecretName)).Returns(validApiKey);

        var context = new DefaultHttpContext();
        context.Request.Headers[Headers.ApiKey] = validApiKey;

        var handler = CreateHandler(context);
        await InitializeHandler(handler, context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
        result.Principal.Identity.AuthenticationType.Should().Be(AuthenticationSchemes.ApiKey);
        result.Principal.Identity.Name.Should().Be("ApiKeyUser");
        
        var claim = result.Principal.FindFirst(ClaimTypes.Name);
        claim.Should().NotBeNull();
        claim!.Value.Should().Be("ApiKeyUser");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithVeryLongApiKey_ShouldSucceed()
    {
        // Arrange
        var longApiKey = new string('a', 1000); // 1000 character key
        var context = new DefaultHttpContext();
        context.Request.Headers[Headers.ApiKey] = longApiKey;

        _mockSecretProvider.Setup(x => x.GetSecret(_settings.ApiKeySecretName)).Returns(longApiKey);

        var handler = CreateHandler(context);
        await InitializeHandler(handler, context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithMultipleHeaderValues_ShouldUseFirstValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[Headers.ApiKey] = new[] { "valid-key", "another-key" };

        _mockSecretProvider.Setup(x => x.GetSecret(_settings.ApiKeySecretName)).Returns("valid-key");

        var handler = CreateHandler(context);
        await InitializeHandler(handler, context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Missing Header Scenarios

    [Fact]
    public async Task HandleAuthenticateAsync_WithMissingHeader_ShouldThrowUnauthorizedWithErrorCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Clear(); // No API Key header

        _mockSecretProvider.Setup(x => x.GetSecret(_settings.ApiKeySecretName)).Returns("valid-key");

        var handler = CreateHandler(context);
        await InitializeHandler(handler, context);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*authentication*required*");
    }

    #endregion

    #region Invalid Key Scenarios

    [Fact]
    public async Task HandleAuthenticateAsync_WithInvalidApiKey_ShouldThrowUnauthorizedWithErrorCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[Headers.ApiKey] = "invalid-key";

        _mockSecretProvider.Setup(x => x.GetSecret(_settings.ApiKeySecretName)).Returns("valid-key");

        var handler = CreateHandler(context);
        await InitializeHandler(handler, context);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*provided credentials*invalid*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task HandleAuthenticateAsync_WithEmptyOrWhitespaceKey_ShouldThrowUnauthorized(string? apiKey)
    {
        // Arrange
        var context = new DefaultHttpContext();
        if (apiKey != null)
            context.Request.Headers[Headers.ApiKey] = apiKey;

        _mockSecretProvider.Setup(x => x.GetSecret(_settings.ApiKeySecretName)).Returns("valid-key");

        var handler = CreateHandler(context);
        await InitializeHandler(handler, context);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_CaseSensitive_ShouldRequireExactMatch()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[Headers.ApiKey] = "Valid-Key"; // Different case

        _mockSecretProvider.Setup(x => x.GetSecret(_settings.ApiKeySecretName)).Returns("valid-key"); // lowercase

        var handler = CreateHandler(context);
        await InitializeHandler(handler, context);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region Configuration Scenarios

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HandleAuthenticateAsync_WhenSecretProviderReturnsNullOrEmpty_ShouldThrowUnauthorized(string? configuredKey)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[Headers.ApiKey] = "some-key";

        _mockSecretProvider.Setup(x => x.GetSecret(_settings.ApiKeySecretName)).Returns(configuredKey);

        var handler = CreateHandler(context);
        await InitializeHandler(handler, context);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WhenSecretProviderThrows_ShouldPropagateException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[Headers.ApiKey] = "some-key";

        _mockSecretProvider
            .Setup(x => x.GetSecret(_settings.ApiKeySecretName))
            .Throws(new InvalidOperationException("Secret store unavailable"));

        var handler = CreateHandler(context);
        await InitializeHandler(handler, context);

        // Act & Assert
        var act = async () => await handler.AuthenticateAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Secret store unavailable");
    }

    #endregion

    private ApiKeyAuthenticationHandler CreateHandler(HttpContext context)
    {
        return new ApiKeyAuthenticationHandler(
            _mockOptions.Object,
            _loggerFactory,
            _encoder,
            _mockSecretProvider.Object,
            _settings);
    }

    private async Task InitializeHandler(AuthenticationHandler<AuthenticationSchemeOptions> handler, HttpContext context)
    {
        await handler.InitializeAsync(
            new AuthenticationScheme(AuthenticationSchemes.ApiKey, null, typeof(ApiKeyAuthenticationHandler)),
            context);
    }
}
