using System.Net;
using System.Text.Json;
using FluentAssertions;
using emc.camus.secrets.dapr.Configurations;
using emc.camus.secrets.dapr.Exceptions;
using emc.camus.secrets.dapr.Services;
using emc.camus.secrets.dapr.test.Helpers;
using static emc.camus.secrets.dapr.test.Helpers.DaprSecretProviderSettingsBuilder;

namespace emc.camus.secrets.dapr.test.Services;

public class DaprSecretProviderTests
{
    private const string ValidSecretValue = "super-secret-value";
    private const string ConnectionRefusedMessage = "connection refused";
    private const string EmptyString = "";
    private const string WhitespaceOnly = "   ";
    private const string AnotherSecretName = "another-secret";
    private const string AnotherSecretValue = "another-secret-value";
    private static readonly string[] SingleValidSecretName = new[] { "my-secret" };
    private static readonly string[] TwoSecretNames = new[] { SingleValidSecretName[0], AnotherSecretName };
    private static readonly string[] WhitespaceOnlyNames = new[] { EmptyString, WhitespaceOnly };
    private static readonly string[] MixedValidAndWhitespaceNames = new[] { SingleValidSecretName[0], "  " };
    private static readonly Dictionary<string, (HttpStatusCode, string)> MultiSecretResponses = new()
    {
        { SingleValidSecretName[0], (HttpStatusCode.OK, JsonSerializer.Serialize(new Dictionary<string, string> { { SingleValidSecretName[0], ValidSecretValue } })) },
        { AnotherSecretName, (HttpStatusCode.OK, JsonSerializer.Serialize(new Dictionary<string, string> { { AnotherSecretName, AnotherSecretValue } })) }
    };
    private static readonly Dictionary<string, string> WrongKeyDictionary = new() { { "wrong-key", "some-value" } };
    public static readonly TheoryData<string[]> WhitespaceContainingNames = new()
    {
        { WhitespaceOnlyNames },
        { MixedValidAndWhitespaceNames }
    };

    private static readonly string[] DefaultProviderSecretNames = [SingleValidSecretName[0]];

    private static DaprSecretProvider CreateProvider(
        DaprSecretProviderSettings? settings = null,
        HttpMessageHandler? handler = null)
    {
        var effectiveSettings = settings ?? CreateValid(secretNames: new List<string>(DefaultProviderSecretNames));
        var effectiveHandler = handler ?? CreateMockHandler(SingleValidSecretName[0], ValidSecretValue);
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

    private static string CreateSecretResponseContent(string secretName, string value) =>
        JsonSerializer.Serialize(new Dictionary<string, string> { { secretName, value } });

    // --- Constructor ---

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        var settings = CreateValid();

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
        var settings = CreateValid();
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        // Act
        _ = new DaprSecretProvider(httpClient, settings);

        // Assert
        httpClient.BaseAddress.Should().NotBeNull();
        httpClient.BaseAddress!.ToString().Should().Contain("localhost");
        httpClient.BaseAddress.ToString().Should().Contain("3500");
        httpClient.BaseAddress.ToString().Should().Contain("my-secret-store");
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    // --- LoadSecretsAsync ---

    [Fact]
    public async Task LoadSecretsAsync_NullSecretNames_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.LoadSecretsAsync(null!, TestContext.Current.CancellationToken);

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
        var act = () => provider.LoadSecretsAsync(Enumerable.Empty<string>(), TestContext.Current.CancellationToken);

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
        var act = () => provider.LoadSecretsAsync(names, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*empty*whitespace*"))
            .And.ParamName.Should().Be("secretNames");
    }

    [Fact]
    public async Task LoadSecretsAsync_ValidSecretName_LoadsSecretSuccessfully()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        await provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

        // Assert
        var result = provider.GetSecret(SingleValidSecretName[0]);
        result.Should().Be(ValidSecretValue);
    }

    [Fact]
    public async Task LoadSecretsAsync_MultipleSecrets_LoadsAllSuccessfully()
    {
        // Arrange
        var handler = CreateMockHandler(MultiSecretResponses);
        var provider = CreateProvider(handler: handler);

        // Act
        await provider.LoadSecretsAsync(TwoSecretNames, TestContext.Current.CancellationToken);

        // Assert
        provider.GetSecret(SingleValidSecretName[0]).Should().Be(ValidSecretValue);
        provider.GetSecret(AnotherSecretName).Should().Be(AnotherSecretValue);
    }

    [Fact]
    public async Task LoadSecretsAsync_SecretNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task LoadSecretsAsync_ServerError_ThrowsDaprSecretStoreException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "error");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DaprSecretStoreException>()
            .WithMessage("*Failed*load*secret*");
    }

    [Theory]
    [InlineData(EmptyString)]
    [InlineData(WhitespaceOnly)]
    public async Task LoadSecretsAsync_EmptyOrWhitespaceResponseContent_ThrowsInvalidOperationException(string responseContent)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

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
        var act = () => provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*null*deserialization*");
    }

    [Fact]
    public async Task LoadSecretsAsync_SecretKeyMissingInResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var responseContent = JsonSerializer.Serialize(WrongKeyDictionary);
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*response*dictionary*");
    }

    [Fact]
    public async Task LoadSecretsAsync_InvalidJsonResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "not valid json {{");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not valid JSON*");
    }

    [Theory]
    [InlineData(EmptyString)]
    [InlineData(WhitespaceOnly)]
    public async Task LoadSecretsAsync_EmptyOrWhitespaceSecretValue_ThrowsInvalidOperationException(string secretValue)
    {
        // Arrange
        var responseContent = CreateSecretResponseContent(SingleValidSecretName[0], secretValue);
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty*whitespace*");
    }

    [Fact]
    public async Task LoadSecretsAsync_HttpClientThrows_ThrowsDaprSecretStoreExceptionWithInnerException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(new HttpRequestException(ConnectionRefusedMessage));
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<DaprSecretStoreException>()
            .WithMessage("*Failed*load*secret*"))
            .And.InnerException.Should().BeOfType<HttpRequestException>();
    }

    // --- GetSecret ---

    [Theory]
    [InlineData(null)]
    [InlineData(EmptyString)]
    [InlineData(WhitespaceOnly)]
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
        await provider.LoadSecretsAsync(SingleValidSecretName, TestContext.Current.CancellationToken);

        // Act
        var result = provider.GetSecret(SingleValidSecretName[0]);

        // Assert
        result.Should().Be(ValidSecretValue);
    }

    // --- CheckConnectivityAsync ---

    [Fact]
    public async Task CheckConnectivityAsync_SecretStoreReachable_DoesNotThrow()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CheckConnectivityAsync_ClientErrorStatusCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Forbidden, "");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed*check*connectivity*403*");
    }

    [Fact]
    public async Task CheckConnectivityAsync_ServerErrorStatusCode_ThrowsDaprSecretStoreException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<DaprSecretStoreException>()
            .WithMessage("*Failed*check*connectivity*"))
            .And.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task CheckConnectivityAsync_HttpClientThrows_ThrowsDaprSecretStoreExceptionWithInnerException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(new HttpRequestException(ConnectionRefusedMessage));
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.CheckConnectivityAsync(TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<DaprSecretStoreException>()
            .WithMessage("*Failed*check*connectivity*"))
            .And.InnerException.Should().BeOfType<HttpRequestException>();
    }

}
