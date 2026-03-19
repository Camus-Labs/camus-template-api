using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using emc.camus.application.Secrets;
using Microsoft.Extensions.Logging;
using emc.camus.secrets.dapr.Configurations;

namespace emc.camus.secrets.dapr.Services
{
    /// <summary>
    /// Provides secret management using Dapr's secret store API.
    /// </summary>
    /// <remarks>
    /// This implementation loads secrets from a Dapr sidecar using HTTP requests and caches them in memory.
    /// </remarks>
    public partial class DaprSecretProvider : ISecretProvider
    {
        private const int MaxRetryAttempts = 3;
        private const int RetryBaseDelayMilliseconds = 500;
        private const int RetryBackoffExponentialBase = 2;
        private const string DaprProtocol = "http://";
        private const string DaprSecretsApiPath = "/v1.0/secrets/";
        private const string TimeoutErrorIndicator = "timeout";
        private const string ConnectionErrorIndicator = "connection";
        private const string NetworkErrorIndicator = "network";

        private readonly ILogger<DaprSecretProvider> _logger;
        private readonly HttpClient _httpClient;
        private readonly DaprSecretProviderSettings _settings;
        private readonly ConcurrentDictionary<string, string> _secrets = new();

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Secret '{SecretName}' not found in loaded secrets. Available secrets: {LoadedCount}")]
        private partial void LogSecretNotFoundInCache(string secretName, int loadedCount);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Secret '{SecretName}' not found in store '{SecretStore}'")]
        private partial void LogSecretNotFoundInStore(string secretName, string secretStore);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Empty response received for secret '{SecretName}'")]
        private partial void LogEmptySecretResponse(string secretName);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Failed to deserialize secret '{SecretName}' response: {ResponseContent}")]
        private partial void LogDeserializationFailed(Exception ex, string secretName, string responseContent);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Transient error on final attempt loading secret '{SecretName}'. Max retries exhausted.")]
        private partial void LogTransientErrorExhausted(Exception ex, string secretName);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Non-retryable error loading secret '{SecretName}' (attempt {Attempt}).")]
        private partial void LogNonRetryableError(Exception ex, string secretName, int attempt);

        /// <summary>
        /// Initializes a new instance of the <see cref="DaprSecretProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to communicate with the Dapr sidecar.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <param name="settings">The configuration settings for the Dapr secret provider.</param>
        public DaprSecretProvider(HttpClient httpClient, ILogger<DaprSecretProvider> logger, DaprSecretProviderSettings settings)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(settings);

            _httpClient = httpClient;
            _logger = logger;
            _settings = settings;

            var baseUrl = $"{DaprProtocol}{_settings.BaseHost}:{_settings.HttpPort}";

            // Configure HttpClient with adapter-specific settings
            _httpClient.BaseAddress = new Uri($"{baseUrl}{DaprSecretsApiPath}{_settings.SecretStoreName}/");
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        /// <summary>
        /// Loads the specified secrets from the Dapr secret store asynchronously.
        /// </summary>
        /// <param name="secretNames">A collection of secret names to load.</param>
        /// <returns>A task representing the asynchronous load operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="secretNames"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when one or more secrets fail to load.</exception>
        public async Task LoadSecretsAsync(IEnumerable<string> secretNames)
        {
            ArgumentNullException.ThrowIfNull(secretNames);

            var secretNamesList = secretNames.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            if (secretNamesList.Count == 0)
            {
                return;
            }

            // Load secrets sequentially with retry logic
            foreach (var secretName in secretNamesList)
            {
                await LoadSingleSecretWithRetryAsync(secretName);
            }

            var successCount = _secrets.Count;
            var failureCount = secretNamesList.Count - successCount;

            if (failureCount > 0)
            {
                var failedSecrets = secretNamesList.Except(_secrets.Keys).ToList();
                var errorMessage = $"Failed to load {failureCount} required secret(s): {string.Join(", ", failedSecrets)}. " +
                    $"Troubleshooting: 1) Verify Dapr sidecar is running at {_settings.BaseHost}:{_settings.HttpPort}, " +
                    $"2) Check secret store '{_settings.SecretStoreName}' is configured in Dapr, " +
                    $"3) Verify secrets exist in the secret store, " +
                    $"4) Check Dapr logs for additional details.";
                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        /// Retrieves the value of a loaded secret by name.
        /// </summary>
        /// <param name="name">The name of the secret to retrieve.</param>
        /// <returns>The secret value if found; otherwise, <c>null</c>.</returns>
        public string? GetSecret(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            // Try to get the secret value from the in-memory cache

            if (_secrets.TryGetValue(name, out var value))
            {
                return value;
            }

            LogSecretNotFoundInCache(name, _secrets.Count);
            return null;
        }

        /// <summary>
        /// Loads a single secret from the Dapr secret store via HTTP.
        /// </summary>
        /// <param name="secretName">The name of the secret to load.</param>
        /// <returns>A task representing the asynchronous load operation.</returns>
        /// <remarks>
        /// Handles 404 responses gracefully (secret not found). Throws exceptions for other HTTP errors.
        /// Validates response content is not empty before parsing.
        /// </remarks>
        private async Task LoadSingleSecretAsync(string secretName)
        {
            // Use relative URL - BaseAddress is configured in constructor
            using var response = await _httpClient.GetAsync(secretName);

            // Handle 404 as acceptable - secret doesn't exist
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                LogSecretNotFoundInStore(secretName, _settings.SecretStoreName);
                return;
            }

            // Ensure success status for other status codes
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                LogEmptySecretResponse(secretName);
                return;
            }


            try
            {
                ParseAndStoreSecret(secretName, responseContent);
            }
            catch (JsonException ex)
            {
                LogDeserializationFailed(ex, secretName, responseContent);
                throw; // Re-throw to trigger retry logic
            }
        }

        /// <summary>
        /// Parses the Dapr secret response JSON and stores the secret value in the in-memory cache.
        /// </summary>
        /// <param name="secretName">The name of the secret being parsed.</param>
        /// <param name="responseContent">The JSON response content from Dapr containing the secret.</param>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails, secret key is not found in response, or secret value is empty.</exception>
        /// <remarks>
        /// Expects Dapr JSON format: { "secretName": "secretValue" }.
        /// Validates that the secret value is not null, empty, or whitespace.
        /// </remarks>
        private void ParseAndStoreSecret(string secretName, string responseContent)
        {
            // Dapr returns JSON: { "secretName": "value" }
            var secretData = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);

            if (secretData == null)
            {
                throw new InvalidOperationException($"Secret '{secretName}' response was null after deserialization");
            }

            if (!secretData.TryGetValue(secretName, out var secretValue))
            {
                throw new InvalidOperationException($"Secret '{secretName}' not found in response dictionary. Available keys: {string.Join(", ", secretData.Keys)}");
            }

            if (string.IsNullOrWhiteSpace(secretValue))
            {
                throw new InvalidOperationException($"Secret '{secretName}' contains empty or whitespace-only value");
            }

            _secrets[secretName] = secretValue;
        }

        /// <summary>
        /// Determines whether an HTTP request exception represents a transient error that can be retried.
        /// </summary>
        /// <param name="ex">The HTTP request exception to analyze.</param>
        /// <returns><c>true</c> if the error is transient (timeout, connection, network issues); otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Checks the exception message for common transient error indicators such as timeout, connection, or network issues.
        /// </remarks>
        private static bool IsTransientError(HttpRequestException ex)
        {
            // Check for common transient HTTP errors in message (network/timeout issues)
            return ex.Message.Contains(TimeoutErrorIndicator, StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains(ConnectionErrorIndicator, StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains(NetworkErrorIndicator, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Loads a single secret with exponential backoff retry logic for transient errors.
        /// </summary>
        /// <param name="secretName">The name of the secret to load.</param>
        /// <returns>A task representing the asynchronous load operation with retry.</returns>
        /// <remarks>
        /// Retries up to <see cref="MaxRetryAttempts"/> times for transient HTTP errors.
        /// Uses exponential backoff with base delay of <see cref="RetryBaseDelayMilliseconds"/> milliseconds.
        /// Non-transient errors fail immediately without retry.
        /// </remarks>
        private async Task LoadSingleSecretWithRetryAsync(string secretName)
        {
            for (int attempt = 0; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    await LoadSingleSecretAsync(secretName);
                    return; // Success
                }
                catch (HttpRequestException ex) when (IsTransientError(ex) && attempt < MaxRetryAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(RetryBaseDelayMilliseconds * Math.Pow(RetryBackoffExponentialBase, attempt));

                    await Task.Delay(delay);
                }
                catch (HttpRequestException ex) when (IsTransientError(ex))
                {
                    // Transient error on final attempt - re-throw to caller
                    LogTransientErrorExhausted(ex, secretName);
                }
                catch (Exception ex)
                {
                    // Non-transient error - stop immediately
                    LogNonRetryableError(ex, secretName, attempt + 1);
                    break;
                }
            }
        }
    }
}
