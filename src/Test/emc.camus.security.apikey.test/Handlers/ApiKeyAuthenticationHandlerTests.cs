using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Secrets;
using emc.camus.security.apikey.Configurations;
using emc.camus.security.apikey.Handlers;

namespace emc.camus.security.apikey.test.Handlers;

public class ApiKeyAuthenticationHandlerTests
{
    private const string ValidApiKey = "test-api-key-12345";
    private const string ValidSecretName = "XApiKey";

    private readonly Mock<ISecretProvider> _secretProviderMock = new();
    private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _optionsMonitorMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();

    private static ApiKeySettings CreateSettings(string secretName = ValidSecretName)
    {
        return new ApiKeySettings { ApiKeySecretName = secretName };
    }

    private async Task<ApiKeyAuthenticationHandler> CreateInitializedHandler(
        HttpContext httpContext,
        ApiKeySettings? settings = null)
    {
        _optionsMonitorMock.Setup(o => o.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var handlerSettings = settings ?? CreateSettings();

        var handler = new ApiKeyAuthenticationHandler(
            _optionsMonitorMock.Object,
            _loggerFactoryMock.Object,
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
        // Arrange
        // Act
        var act = () => new ApiKeyAuthenticationHandler(
            _optionsMonitorMock.Object,
            _loggerFactoryMock.Object,
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
        // Arrange
        // Act
        var act = () => new ApiKeyAuthenticationHandler(
            _optionsMonitorMock.Object,
            _loggerFactoryMock.Object,
            UrlEncoder.Default,
            _secretProviderMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("settings");
    }

    // --- HandleAuthenticateAsync (via AuthenticateAsync) ---

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
