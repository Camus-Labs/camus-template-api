using System.Net;
using System.Text.Json;
using FluentAssertions;
using emc.camus.secrets.dapr.Configurations;
using emc.camus.secrets.dapr.Services;

namespace emc.camus.secrets.dapr.test.Services;

public class DaprSecretProviderTests
{
    private const string ValidBaseHost = "localhost";
    private const string ValidHttpPort = "3500";
    private const string ValidSecretStoreName = "my-secret-store";
    private const int ValidTimeoutSeconds = 30;
    private const string ValidSecretName = "my-secret";
    private const string ValidSecretValue = "super-secret-value";
    private const string AnotherSecretName = "another-secret";
    private const string AnotherSecretValue = "another-secret-value";
    private static readonly string[] WhitespaceOnlyNames = new[] { "", "   " };

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
        await act.Should().ThrowAsync<ArgumentNullException>()
            .Where(ex => ex.ParamName == "secretNames");
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

    [Fact]
    public async Task LoadSecretsAsync_WhitespaceOnlyNames_ThrowsArgumentException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.LoadSecretsAsync(WhitespaceOnlyNames);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*null*whitespace*")
            .Where(ex => ex.ParamName == "secretNames");
    }

    [Fact]
    public async Task LoadSecretsAsync_MixedValidAndWhitespaceNames_ThrowsArgumentException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName, "  " });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*null*whitespace*");
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
        var responses = new Dictionary<string, (HttpStatusCode, string)>
        {
            { ValidSecretName, (HttpStatusCode.OK, JsonSerializer.Serialize(new Dictionary<string, string> { { ValidSecretName, ValidSecretValue } })) },
            { AnotherSecretName, (HttpStatusCode.OK, JsonSerializer.Serialize(new Dictionary<string, string> { { AnotherSecretName, AnotherSecretValue } })) }
        };
        var handler = CreateMockHandler(responses);
        var provider = CreateProvider(handler: handler);

        // Act
        await provider.LoadSecretsAsync(new[] { ValidSecretName, AnotherSecretName });

        // Assert
        provider.GetSecret(ValidSecretName).Should().Be(ValidSecretValue);
        provider.GetSecret(AnotherSecretName).Should().Be(AnotherSecretValue);
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

    [Fact]
    public async Task LoadSecretsAsync_EmptyResponseContent_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "");
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Empty*response*");
    }

    [Fact]
    public async Task LoadSecretsAsync_WhitespaceResponseContent_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "   ");
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

    [Fact]
    public async Task LoadSecretsAsync_EmptySecretValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var responseContent = JsonSerializer.Serialize(
            new Dictionary<string, string> { { ValidSecretName, "" } });
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseContent);
        var provider = CreateProvider(handler: handler);

        // Act
        var act = () => provider.LoadSecretsAsync(new[] { ValidSecretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty*whitespace*");
    }

    [Fact]
    public async Task LoadSecretsAsync_WhitespaceSecretValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var responseContent = JsonSerializer.Serialize(
            new Dictionary<string, string> { { ValidSecretName, "   " } });
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

    // --- FakeHttpMessageHandler ---

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _statusCode;
        private readonly string? _content;
        private readonly Exception? _exception;
        private readonly Dictionary<string, (HttpStatusCode statusCode, string content)>? _responses;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        public FakeHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        public FakeHttpMessageHandler(Dictionary<string, (HttpStatusCode statusCode, string content)> responses)
        {
            _responses = responses;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            if (_responses != null)
            {
                var secretName = request.RequestUri!.Segments.Last();
                if (_responses.TryGetValue(secretName, out var response))
                {
                    return Task.FromResult(new HttpResponseMessage(response.statusCode)
                    {
                        Content = new StringContent(response.content)
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("")
                });
            }

            return Task.FromResult(new HttpResponseMessage(_statusCode!.Value)
            {
                Content = new StringContent(_content!)
            });
        }
    }
}
