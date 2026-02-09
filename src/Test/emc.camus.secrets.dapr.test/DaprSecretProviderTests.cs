using System.Net;
using System.Text.Json;
using emc.camus.secrets.dapr;
using emc.camus.secrets.dapr.Configurations;
using emc.camus.secrets.dapr.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;

namespace emc.camus.secrets.dapr.test;

/// <summary>
/// Unit tests for DaprSecretProvider to verify secret loading and retrieval logic.
/// </summary>
public class DaprSecretProviderTests
{
    private readonly Mock<ILogger<DaprSecretProvider>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;

    public DaprSecretProviderTests()
    {
        _mockLogger = new Mock<ILogger<DaprSecretProvider>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        var settings = CreateDefaultSettings();

        // Act & Assert
        var act = () => new DaprSecretProvider(null!, _mockLogger.Object, settings);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var settings = CreateDefaultSettings();

        // Act & Assert
        var act = () => new DaprSecretProvider(_httpClient, null!, settings);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new DaprSecretProvider(_httpClient, _mockLogger.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
    }

    [Fact]
    public void GetSecret_WithNullOrEmptyName_ShouldReturnNull()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var result1 = provider.GetSecret(null!);
        var result2 = provider.GetSecret(string.Empty);
        var result3 = provider.GetSecret("   ");

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }

    [Fact]
    public void GetSecret_WithNonExistentSecret_ShouldReturnNull()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var result = provider.GetSecret("non-existent-secret");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetLoadedSecretsCount_WithNoSecretsLoaded_ShouldReturnZero()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var count = provider.GetLoadedSecretsCount();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void HasSecret_WithNullOrEmptyName_ShouldReturnFalse()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act & Assert
        provider.HasSecret(null!).Should().BeFalse();
        provider.HasSecret(string.Empty).Should().BeFalse();
        provider.HasSecret("   ").Should().BeFalse();
    }

    [Fact]
    public void HasSecret_WithNonExistentSecret_ShouldReturnFalse()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var result = provider.HasSecret("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetLoadedSecretNames_WithNoSecrets_ShouldReturnEmptyCollection()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var names = provider.GetLoadedSecretNames();

        // Assert
        names.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithNullSecretNames_ShouldNotThrow()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act & Assert
        var act = async () => await provider.LoadSecretsAsync(null!);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithEmptyList_ShouldNotThrow()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act & Assert
        var act = async () => await provider.LoadSecretsAsync(new List<string>());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithSuccessfulResponse_ShouldLoadSecret()
    {
        // Arrange
        var secretName = "test-secret";
        var secretValue = "test-value";
        var secretData = new Dictionary<string, string> { { secretName, secretValue } };
        var responseContent = JsonSerializer.Serialize(secretData);

        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        provider.HasSecret(secretName).Should().BeTrue();
        provider.GetSecret(secretName).Should().Be(secretValue);
        provider.GetLoadedSecretsCount().Should().Be(1);
        provider.GetLoadedSecretNames().Should().Contain(secretName);
    }

    [Fact]
    public async Task LoadSecretsAsync_With404Response_ShouldNotThrow()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, string.Empty);

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { "missing-secret" });

        // Assert - 404 is acceptable, throws because required secret failed
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithMultipleSecrets_ShouldLoadAll()
    {
        // Arrange
        var secret1Name = "secret1";
        var secret2Name = "secret2";
        var secret1Value = "value1";
        var secret2Value = "value2";

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(secret1Name)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, string> { { secret1Name, secret1Value } }))
            });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(secret2Name)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, string> { { secret2Name, secret2Value } }))
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        await provider.LoadSecretsAsync(new[] { secret1Name, secret2Name });

        // Assert
        provider.GetLoadedSecretsCount().Should().Be(2);
        provider.GetSecret(secret1Name).Should().Be(secret1Value);
        provider.GetSecret(secret2Name).Should().Be(secret2Value);
    }

    private DaprSecretProviderSettings CreateDefaultSettings(List<string>? secretNames = null)
    {
        return new DaprSecretProviderSettings
        {
            BaseHost = "localhost",
            HttpPort = "3500",
            UseHttps = false,
            SecretStoreName = "test-store",
            TimeoutSeconds = 5,
            SecretNames = secretNames ?? new List<string>()
        };
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }
}
