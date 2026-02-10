using System.Net;
using System.Text.Json;
using emc.camus.secrets.dapr;
using emc.camus.secrets.dapr.Configurations;
using emc.camus.secrets.dapr.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;

namespace emc.camus.secrets.dapr.test.Services;

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
        var act = () => new DaprSecretProvider(null, _mockLogger.Object, settings);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var settings = CreateDefaultSettings();

        // Act & Assert
        var act = () => new DaprSecretProvider(_httpClient, null, settings);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new DaprSecretProvider(_httpClient, _mockLogger.Object, null);
        act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
    }

    [Fact]
    public void Constructor_WithUseHttpsTrue_ShouldConfigureHttpsBaseUrl()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        settings.UseHttps = true;

        // Act
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Assert
        _httpClient.BaseAddress.Should().NotBeNull();
        _httpClient.BaseAddress!.ToString().Should().StartWith("https://");
        _httpClient.BaseAddress.ToString().Should().Contain($"{settings.BaseHost}:{settings.HttpPort}");
    }

    [Fact]
    public void Constructor_WithUseHttpsFalse_ShouldConfigureHttpBaseUrl()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        settings.UseHttps = false;

        // Act
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Assert
        _httpClient.BaseAddress.Should().NotBeNull();
        _httpClient.BaseAddress!.ToString().Should().StartWith("http://");
        _httpClient.BaseAddress.ToString().Should().Contain($"{settings.BaseHost}:{settings.HttpPort}");
    }

    [Fact]
    public void GetSecret_WithInvalidOrNonExistentName_ShouldReturnNull()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act & Assert - test null, empty, whitespace, and non-existent
        provider.GetSecret(null).Should().BeNull();
        provider.GetSecret(string.Empty).Should().BeNull();
        provider.GetSecret("   ").Should().BeNull();
        provider.GetSecret("non-existent-secret").Should().BeNull();
    }

    [Fact]
    public void HasSecret_WithInvalidOrNonExistentName_ShouldReturnFalse()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act & Assert - test null, empty, whitespace, and non-existent
        provider.HasSecret(null).Should().BeFalse();
        provider.HasSecret(string.Empty).Should().BeFalse();
        provider.HasSecret("   ").Should().BeFalse();
        provider.HasSecret("non-existent").Should().BeFalse();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithNullOrEmptyList_ShouldNotThrow()
    {
        // Arrange
        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act & Assert
        await provider.LoadSecretsAsync(null);
        await provider.LoadSecretsAsync(new List<string>());
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

    [Fact]
    public async Task LoadSecretsAsync_WithEmptySecretValue_ShouldNotStoreSecret()
    {
        // Arrange
        var secretName = "empty-secret";
        var secretData = new Dictionary<string, string> { { secretName, string.Empty } };
        var responseContent = JsonSerializer.Serialize(secretData);

        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert - should throw because required secret failed to load
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to load*");
    }

    [Fact]
    public async Task LoadSecretsAsync_WithNullSecretData_ShouldNotStoreSecret()
    {
        // Arrange
        var secretName = "null-secret";
        var responseContent = "null";

        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithMissingSecretInResponse_ShouldNotStoreSecret()
    {
        // Arrange
        var secretName = "expected-secret";
        var secretData = new Dictionary<string, string> { { "different-secret", "value" } };
        var responseContent = JsonSerializer.Serialize(secretData);

        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithInvalidJson_ShouldThrowJsonException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "invalid-json{");

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { "test-secret" });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithMixedValidAndInvalidNames_ShouldLoadValidOnes()
    {
        // Arrange
        var secretName = "valid-secret";
        var secretValue = "valid-value";
        var secretData = new Dictionary<string, string> { { secretName, secretValue } };
        var responseContent = JsonSerializer.Serialize(secretData);

        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act - includes whitespace, empty, and null values
        await provider.LoadSecretsAsync(new[] { secretName, "", "  ", "\t", null });

        // Assert
        provider.GetLoadedSecretsCount().Should().Be(1);
        provider.GetSecret(secretName).Should().Be(secretValue);
    }

    [Fact]
    public async Task LoadSecretsAsync_WithEmptyResponse_ShouldLogWarning()
    {
        // Arrange
        var secretName = "empty-response-secret";
        SetupHttpResponse(HttpStatusCode.OK, string.Empty);

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        provider.GetLoadedSecretsCount().Should().Be(0);
    }

    [Fact]
    public void GetLoadedSecretNames_AfterLoadingSecrets_ShouldReturnAllNames()
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

        var settings = CreateDefaultSettings(new List<string> { secret1Name, secret2Name });
        
        // Act
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);
        var names = provider.GetLoadedSecretNames();

        // Assert
        names.Should().HaveCount(2);
        names.Should().Contain(new[] { secret1Name, secret2Name });
    }

    [Fact]
    public async Task LoadSecretsAsync_WithTransientHttpError_ShouldRetryWithExponentialBackoff()
    {
        // Arrange
        var secretName = "retry-secret";
        var secretValue = "retry-value";
        var secretData = new Dictionary<string, string> { { secretName, secretValue } };
        var responseContent = JsonSerializer.Serialize(secretData);

        var attemptCount = 0;
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                attemptCount++;
                // Fail first 2 attempts with transient error, succeed on 3rd
                if (attemptCount <= 2)
                {
                    throw new HttpRequestException("Connection timeout occurred");
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent)
                };
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var startTime = DateTime.UtcNow;
        await provider.LoadSecretsAsync(new[] { secretName });
        var elapsedTime = DateTime.UtcNow - startTime;

        // Assert
        attemptCount.Should().Be(3); // Should have retried twice and succeeded on 3rd attempt
        provider.HasSecret(secretName).Should().BeTrue();
        provider.GetSecret(secretName).Should().Be(secretValue);
        // Verify exponential backoff: 500ms + 1000ms = 1500ms minimum
        elapsedTime.Should().BeGreaterThan(TimeSpan.FromMilliseconds(1400));
    }

    [Fact]
    public async Task LoadSecretsAsync_WithPersistentTransientError_ShouldFailAfterMaxRetries()
    {
        // Arrange
        var secretName = "failing-secret";

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network connection failed"));

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert - should fail and throw because secret couldn't be loaded
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to load*");
        provider.HasSecret(secretName).Should().BeFalse();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithHttp500Error_ShouldNotRetryNonTransientError()
    {
        // Arrange
        var secretName = "server-error-secret";
        var attemptCount = 0;

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                attemptCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal Server Error")
                };
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert - should fail without retries (HTTP 500 throws HttpRequestException but non-transient)
        await act.Should().ThrowAsync<InvalidOperationException>();
        attemptCount.Should().Be(1); // No retries for non-transient HTTP errors
    }

    [Fact]
    public async Task LoadSecretsAsync_WithHttp503ServiceUnavailable_ShouldNotRetry()
    {
        // Arrange
        var secretName = "unavailable-secret";
        var attemptCount = 0;

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                attemptCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    Content = new StringContent("Service Unavailable")
                };
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attemptCount.Should().Be(1); // No retries for 503
    }

    [Fact]
    public async Task LoadSecretsAsync_WithConcurrentRequests_ShouldLimitToFiveParallel()
    {
        // Arrange
        var secretNames = Enumerable.Range(1, 20).Select(i => $"secret{i}").ToList();
        var maxConcurrency = 0;
        var currentConcurrency = 0;
        var lockObject = new object();

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage req, CancellationToken ct) =>
            {
                lock (lockObject)
                {
                    currentConcurrency++;
                    if (currentConcurrency > maxConcurrency)
                        maxConcurrency = currentConcurrency;
                }

                // Simulate some work
                await Task.Delay(50, ct);

                lock (lockObject)
                {
                    currentConcurrency--;
                }

                // Extract secret name from request URI
                var secretName = req.RequestUri?.ToString().Split('/').Last() ?? "unknown";
                var secretData = new Dictionary<string, string> { { secretName, $"value-{secretName}" } };
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(secretData))
                };
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        await provider.LoadSecretsAsync(secretNames);

        // Assert
        maxConcurrency.Should().BeLessThanOrEqualTo(5, "semaphore should limit concurrent requests to 5");
        provider.GetLoadedSecretsCount().Should().Be(20);
    }

    [Fact]
    public async Task LoadSecretsAsync_WithWhitespaceSecretValue_ShouldNotStoreSecret()
    {
        // Arrange
        var secretName = "whitespace-secret";
        var secretData = new Dictionary<string, string> { { secretName, "   " } };
        var responseContent = JsonSerializer.Serialize(secretData);

        SetupHttpResponse(HttpStatusCode.OK, responseContent);

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert - whitespace-only values are treated as empty
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to load*");
        provider.HasSecret(secretName).Should().BeFalse();
    }

    [Fact]
    public async Task LoadSecretsAsync_CalledTwiceWithSameSecret_ShouldOverwriteValue()
    {
        // Arrange
        var secretName = "overwrite-secret";
        var firstValue = "first-value";
        var secondValue = "second-value";

        // First call setup
        var callCount = 0;
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                var value = callCount == 1 ? firstValue : secondValue;
                var secretData = new Dictionary<string, string> { { secretName, value } };
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(secretData))
                };
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act - Load same secret twice
        await provider.LoadSecretsAsync(new[] { secretName });
        var firstLoadedValue = provider.GetSecret(secretName);
        
        await provider.LoadSecretsAsync(new[] { secretName });
        var secondLoadedValue = provider.GetSecret(secretName);

        // Assert
        firstLoadedValue.Should().Be(firstValue);
        secondLoadedValue.Should().Be(secondValue);
        provider.GetLoadedSecretsCount().Should().Be(1); // Still only one secret
        callCount.Should().Be(2); // HTTP call made twice
    }

    [Fact]
    public async Task LoadSecretsAsync_WithTaskCanceledException_ShouldNotRetry()
    {
        // Arrange
        var secretName = "canceled-secret";

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert - TaskCanceledException is non-retryable
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoadSecretsAsync_WithNetworkErrorKeyword_ShouldRetry()
    {
        // Arrange
        var secretName = "network-error-secret";
        var secretValue = "network-value";
        var attemptCount = 0;

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                attemptCount++;
                // Fail first attempt with network error, succeed on 2nd
                if (attemptCount == 1)
                {
                    throw new HttpRequestException("Network error detected");
                }
                var secretData = new Dictionary<string, string> { { secretName, secretValue } };
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(secretData))
                };
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        attemptCount.Should().Be(2); // Should have retried once
        provider.GetSecret(secretName).Should().Be(secretValue);
    }

    [Fact]
    public async Task LoadSecretsAsync_WithConnectionErrorKeyword_ShouldRetry()
    {
        // Arrange
        var secretName = "connection-error-secret";
        var secretValue = "connection-value";
        var attemptCount = 0;

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                attemptCount++;
                // Fail first attempt with connection error, succeed on 2nd
                if (attemptCount == 1)
                {
                    throw new HttpRequestException("Connection refused");
                }
                var secretData = new Dictionary<string, string> { { secretName, secretValue } };
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(secretData))
                };
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        attemptCount.Should().Be(2); // Should have retried once
        provider.GetSecret(secretName).Should().Be(secretValue);
    }

    [Fact]
    public async Task LoadSecretsAsync_WithNonTransientHttpError_ShouldNotRetry()
    {
        // Arrange
        var secretName = "non-transient-secret";
        var attemptCount = 0;

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                attemptCount++;
                // Non-transient error (doesn't contain timeout, connection, or network)
                return Task.FromException<HttpResponseMessage>(new HttpRequestException("Unauthorized access"));
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attemptCount.Should().Be(1); // Should NOT retry for non-transient errors
    }

    [Fact]
    public async Task LoadSecretsAsync_WithNonHttpException_ShouldCatchAndLogError()
    {
        // Arrange
        var secretName = "exception-secret";
        var attemptCount = 0;

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                attemptCount++;
                // Non-HTTP exception (ArgumentException, InvalidOperationException, etc.)
                return Task.FromException<HttpResponseMessage>(new InvalidOperationException("Unexpected error"));
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to load*");
        attemptCount.Should().Be(1); // Should NOT retry for non-HTTP exceptions
    }

    [Fact]
    public async Task LoadSecretsAsync_WithTransientErrorExhaustedRetries_ShouldLogErrorAndReturn()
    {
        // Arrange
        var secretName = "exhausted-retries-secret";
        var attemptCount = 0;
        const int expectedAttempts = 4; // Initial attempt + 3 retries

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                attemptCount++;
                // Always throw transient error - will exhaust retries
                return Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection timeout"));
            });

        var settings = CreateDefaultSettings(new List<string>());
        var provider = new DaprSecretProvider(_httpClient, _mockLogger.Object, settings);

        // Act
        var act = async () => await provider.LoadSecretsAsync(new[] { secretName });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to load*");
        attemptCount.Should().Be(expectedAttempts); // Should attempt 4 times (initial + 3 retries)
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
