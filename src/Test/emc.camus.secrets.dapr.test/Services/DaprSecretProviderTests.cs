using System.Net;
using System.Text.Json;
using FluentAssertions;
using emc.camus.secrets.dapr.Configurations;
using emc.camus.secrets.dapr.Services;
using emc.camus.secrets.dapr.test.Helpers;

namespace emc.camus.secrets.dapr.test.Services;

public class DaprSecretProviderTests
{
    private const string ValidBaseHost = "localhost";
    private const string ValidHttpPort = "3500";
    private const string ValidSecretStoreName = "my-secret-store";
    private const int ValidTimeoutSeconds = 30;
    private const string ValidSecretName = "my-secret";
    private const string ValidSecretValue = "super-secret-value";
    private static readonly string[] WhitespaceOnlyNames = new[] { "", "   " };
    private static readonly string[] MixedValidAndWhitespaceNames = new[] { ValidSecretName, "  " };
    public static TheoryData<string[]> WhitespaceContainingNames => new()
    {
        { WhitespaceOnlyNames },
        { MixedValidAndWhitespaceNames }
    };

    private static DaprSecretProviderSettings CreateValidSettings() => new()
    {
        BaseHost = ValidBaseHost,
        HttpPort = ValidHttpPort,
        SecretStoreName = ValidSecretStoreName,
        TimeoutSeconds = ValidTimeoutSeconds,
        SecretNames = new List<string> { ValidSecretName }
    };

    private static DaprSecretProvider CreateProvider(
        DaprSecretProviderSettings? settings = null,
        HttpMessageHandler? handler = null)
    {
        var effectiveSettings = settings ?? CreateValidSettings();
        var effectiveHandler = handler ?? CreateMockHandler(ValidSecretName, ValidSecretValue);
        var httpClient = new HttpClient(effectiveHandler);
        return new DaprSecretProvider(httpClient, effectiveSettings);
    }

    private static FakeHttpMessageHandler CreateMockHandler(string secretName, string secretValue)
    {
        var responseContent = JsonSerializer.Serialize(
            new Dictionary<string, string> { { secretName, secretValue } });
        return new FakeHttpMessageHandler(HttpStatusCode.OK, responseContent);
    }

    private static FakeHttpMessageHandler CreateMockHandler(Dictionary<string, (HttpStatusCode statusCode, string content)> responses)
    {
        return new FakeHttpMessageHandler(responses);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = CreateValidSettings();

        // Act
        var act = () => new DaprSecretProvider(null!, settings);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("httpClient");
    }

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var act = () => new DaprSecretProvider(httpClient, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("settings");
    }

    [Fact]
    public void Constructor_ValidParameters_ConfiguresHttpClient()
    {
        // Arrange
        var settings = CreateValidSettings();
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        // Act
        var provider = new DaprSecretProvider(httpClient, settings);

        // Assert
        httpClient.BaseAddress.Should().NotBeNull();
        httpClient.BaseAddress!.ToString().Should().Contain(ValidBaseHost);
        httpClient.BaseAddress.ToString().Should().Contain(ValidHttpPort);
        httpClient.BaseAddress.ToString().Should().Contain(ValidSecretStoreName);
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(ValidTimeoutSeconds));
    }

    // --- LoadSecretsAsync ---

    [Fact]
    public async Task LoadSecretsAsync_NullSecretNames_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.LoadSecretsAsync(null!);

        // Assert
        (await act.Should().ThrowAsync<ArgumentNullException>())
            .And.ParamName.Should().Be("secretNames");
    }

    [Fact]
    public async Task LoadSecretsAsync_EmptySecretNames_DoesNotThrow()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.LoadSecretsAsync(Enumerable.Empty<string>());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [MemberData(nameof(WhitespaceContainingNames))]
    public async Task LoadSecretsAsync_NamesContainingWhitespace_ThrowsArgumentException(string[] names)
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.LoadSecretsAsync(names);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*null*whitespace*"))
            .And.ParamName.Should().Be("secretNames");
    }

    [Fact]
    public async Task LoadSecretsAsync_ValidSecretName_LoadsSecretSuccessfully()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        await provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        var result = provider.GetSecret(ValidSecretName);
        result.Should().Be(ValidSecretValue);
    }

    [Fact]
    public async Task LoadSecretsAsync_MultipleSecrets_LoadsAllSuccessfully()
    {
        // Arrange
        var anotherSecretName = "another-secret";
        var anotherSecretValue = "another-secret-value";
        var responses = new Dictionary<string, (HttpStatusCode, string)>
        {
            { ValidSecretName, (HttpStatusCode.OK, JsonSerializer.Serialize(new Dictionary<string, string> { { ValidSecretName, ValidSecretValue } })) },
            { anotherSecretName, (HttpStatusCode.OK, JsonSerializer.Serialize(new Dictionary<string, string> { { anotherSecretName, anotherSecretValue } })) }
        };
        var handler = CreateMockHandler(responses);
        var provider = CreateProvider(handler: handler);

        // Act
        await provider.LoadSecretsAsync(new[] { ValidSecretName, anotherSecretName });

        // Assert
        provider.GetSecret(ValidSecretName).Should().Be(ValidSecretValue);
        provider.GetSecret(anotherSecretName).Should().Be(anotherSecretValue);
    }

    [Fact]
    public async Task LoadSecretsAsync_SecretNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task LoadSecretsAsync_ServerError_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "error");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed*load*secret*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoadSecretsAsync_EmptyOrWhitespaceResponseContent_ThrowsInvalidOperationException(string responseContent)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Empty*response*");
    }

    [Fact]
    public async Task LoadSecretsAsync_NullDeserialization_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "null");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*null*deserialization*");
    }

    [Fact]
    public async Task LoadSecretsAsync_SecretKeyMissingInResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var responseContent = JsonSerializer.Serialize(
            new Dictionary<string, string> { { "wrong-key", "some-value" } });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*response*dictionary*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoadSecretsAsync_EmptyOrWhitespaceSecretValue_ThrowsInvalidOperationException(string secretValue)
    {
        // Arrange
        var responseContent = JsonSerializer.Serialize(
            new Dictionary<string, string> { { ValidSecretName, secretValue } });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty*whitespace*");
    }

    [Fact]
    public async Task LoadSecretsAsync_HttpClientThrows_ThrowsInvalidOperationExceptionWithInnerException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(new HttpRequestException("connection refused"));
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed*load*secret*"))
            .And.InnerException.Should().BeOfType<HttpRequestException>();
    }

    // --- GetSecret ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSecret_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.GetSecret(name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetSecret_SecretNotLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.GetSecret("non-existent-secret");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found*loaded*secrets*");
    }

    [Fact]
    public async Task GetSecret_SecretLoaded_ReturnsValue()
    {
        // Arrange
        var provider = CreateProvider();
        await provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Act
        var result = provider.GetSecret(ValidSecretName);

        // Assert
        result.Should().Be(ValidSecretValue);
    }

}
