using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Secrets;
using emc.camus.api.Configurations;
using emc.camus.api.Utilities;
using emc.camus.api.test.Helpers;

namespace emc.camus.api.test.Utilities;

public class ApiKeyAuthenticationHandlerTests
{
    private const string ValidApiKey = "test-api-key-12345";
    private const string ValidSecretName = "XApiKey";

    private readonly Mock<ISecretProvider> _secretProviderMock;
    private readonly IOptionsMonitor<AuthenticationSchemeOptions> _optionsMonitor;
    private readonly ILoggerFactory _loggerFactory;

    public ApiKeyAuthenticationHandlerTests()
    {
        _secretProviderMock = new Mock<ISecretProvider>();
        _optionsMonitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
        _loggerFactory = NullLoggerFactory.Instance;
    }

    private static ApiKeySettings CreateSettings(string secretName = ValidSecretName)
    {
        return new ApiKeySettings { ApiKeySecretName = secretName };
    }

    private async Task<ApiKeyAuthenticationHandler> CreateInitializedHandler(
        HttpContext httpContext,
        ApiKeySettings? settings = null)
    {
        var handlerSettings = settings ?? CreateSettings();

        var handler = new ApiKeyAuthenticationHandler(
            _optionsMonitor,
            _loggerFactory,
            UrlEncoder.Default,
            _secretProviderMock.Object,
            handlerSettings);

        var scheme = new AuthenticationScheme(
            AuthenticationSchemes.ApiKey,
            displayName: null,
            handlerType: typeof(ApiKeyAuthenticationHandler));

        await handler.InitializeAsync(scheme, httpContext);
        return handler;
    }

    private static DefaultHttpContext CreateHttpContext(string? apiKeyHeaderValue = null)
    {
        var context = new DefaultHttpContext();
        if (apiKeyHeaderValue is not null)
        {
            context.Request.Headers[Headers.ApiKey] = apiKeyHeaderValue;
        }
        return context;
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullSecretProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ApiKeyAuthenticationHandler(
            _optionsMonitor,
            _loggerFactory,
            UrlEncoder.Default,
            null!,
            CreateSettings());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("secretProvider");
    }

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ApiKeyAuthenticationHandler(
            _optionsMonitor,
            _loggerFactory,
            UrlEncoder.Default,
            _secretProviderMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("settings");
    }

    // --- HandleAuthenticateAsync (via AuthenticateAsync) --- AC-03, AC-04

    [Fact]
    public async Task AuthenticateAsync_MissingApiKeyHeader_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var context = CreateHttpContext();
        var handler = await CreateInitializedHandler(context);

        // Act
        var act = () => handler.AuthenticateAsync();

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*authentication*required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("wrong-key")]
    public async Task AuthenticateAsync_InvalidCredentials_ThrowsUnauthorizedAccessException(
        string apiKeyHeaderValue)
    {
        // Arrange
        var context = CreateHttpContext(apiKeyHeaderValue: apiKeyHeaderValue);
        _secretProviderMock.Setup(s => s.GetSecret(ValidSecretName)).Returns(ValidApiKey);
        var handler = await CreateInitializedHandler(context);

        // Act
        var act = () => handler.AuthenticateAsync();

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*credentials*invalid*");
    }

    [Fact]
    public async Task AuthenticateAsync_ValidApiKey_ReturnsSuccessWithCorrectPrincipal()
    {
        // Arrange
        var context = CreateHttpContext(apiKeyHeaderValue: ValidApiKey);
        _secretProviderMock.Setup(s => s.GetSecret(ValidSecretName)).Returns(ValidApiKey);
        var handler = await CreateInitializedHandler(context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Ticket!.AuthenticationScheme.Should().Be(AuthenticationSchemes.ApiKey);
        result.Principal!.Identity!.Name.Should().Be(ApiKeySettings.DefaultUsername);
        result.Principal!.Identity!.AuthenticationType.Should().Be(AuthenticationSchemes.ApiKey);
        result.Principal!.Claims.Should().ContainSingle(c =>
            c.Type == ClaimTypes.Name && c.Value == ApiKeySettings.DefaultUsername);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidApiKey_SetsNameIdentifierClaim()
    {
        // Arrange
        var context = CreateHttpContext(apiKeyHeaderValue: ValidApiKey);
        _secretProviderMock.Setup(s => s.GetSecret(ValidSecretName)).Returns(ValidApiKey);
        var handler = await CreateInitializedHandler(context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Principal!.Claims.Should().ContainSingle(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == ApiKeySettings.DefaultUserId);
    }

    [Fact]
    public async Task AuthenticateAsync_CustomSecretName_UsesConfiguredSecretName()
    {
        // Arrange
        var customSecretName = "CustomApiKeySecret";
        var context = CreateHttpContext(apiKeyHeaderValue: ValidApiKey);
        _secretProviderMock.Setup(s => s.GetSecret(customSecretName)).Returns(ValidApiKey);
        var settings = CreateSettings(secretName: customSecretName);
        var handler = await CreateInitializedHandler(context, settings);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}
