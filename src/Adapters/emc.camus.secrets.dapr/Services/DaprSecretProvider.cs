using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using emc.camus.application.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using emc.camus.secrets.dapr.Configurations;

namespace emc.camus.secrets.dapr.Services
{
    /// <summary>
    /// Provides secret management using Dapr's secret store API.
    /// </summary>
    /// <remarks>
    /// This implementation loads secrets from a Dapr sidecar using HTTP requests and caches them in memory.
    /// </remarks>
    public class DaprSecretProvider : ISecretProvider
    {
        private readonly ILogger<DaprSecretProvider> _logger;
        private readonly HttpClient _httpClient;
        private readonly DaprSecretProviderSettings _settings;
        private readonly ConcurrentDictionary<string, string> _secrets = new();


        /// <summary>
        /// Initializes a new instance of the <see cref="DaprSecretProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to communicate with the Dapr sidecar.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <param name="settings">The configuration settings for the Dapr secret provider.</param>
        public DaprSecretProvider(HttpClient httpClient, ILogger<DaprSecretProvider> logger, DaprSecretProviderSettings settings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
           
            var protocol = _settings.UseHttps ? "https" : "http";
            var baseUrl = $"{protocol}://{_settings.BaseHost}:{_settings.HttpPort}";
            
            // Configure HttpClient with adapter-specific settings
            _httpClient.BaseAddress = new Uri($"{baseUrl}/v1.0/secrets/{_settings.SecretStoreName}/");
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
            
            _logger.LogInformation("DaprSecretProvider initialized with base Host: {BaseHost}, Secret Store: {SecretStore}, Timeout: {Timeout}s", _settings.BaseHost, _settings.SecretStoreName, _settings.TimeoutSeconds);
            
            // Load secrets from settings during construction
            LoadSecretsAsync(_settings.SecretNames).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Loads the specified secrets from the Dapr secret store asynchronously.
        /// </summary>
        /// <param name="secretNames">A collection of secret names to load.</param>
        /// <returns>A task representing the asynchronous load operation.</returns>
        public async Task LoadSecretsAsync(IEnumerable<string> secretNames)
        {
            // Filter out null/empty names - settings validation ensures this won't be empty at startup
            var secretNamesList = secretNames?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>();
            
            if (secretNamesList.Count == 0)
            {
                _logger.LogInformation("No valid secret names provided to load");
                return;
            }

            _logger.LogInformation("Starting to load {Count} secrets from Dapr secret store", secretNamesList.Count);


            // Process secrets in parallel with resilience
            var semaphore = new SemaphoreSlim(5, 5); // Limit concurrent requests to 5
            var loadTasks = secretNamesList.Select(async secretName =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await LoadSingleSecretWithRetryAsync(secretName);
                }
                finally
                {
                    semaphore.Release();
                }
            });


            await Task.WhenAll(loadTasks);
           
            var successCount = _secrets.Count;
            var failureCount = secretNamesList.Count - successCount;
            _logger.LogInformation("Secret loading completed. Success: {SuccessCount}, Failed: {FailureCount}", successCount, failureCount);
            
            if (failureCount > 0)
            {
                var failedSecrets = secretNamesList.Except(_secrets.Keys).ToList();
                var errorMessage = $"Failed to load {failureCount} required secret(s): {string.Join(", ", failedSecrets)}";
                _logger.LogError(errorMessage);
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
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("GetSecret called with null or empty name");
                return null;
            }

            if (_secrets.TryGetValue(name, out var value))
            {
                _logger.LogDebug("Secret '{SecretName}' retrieved successfully", name);
                return value;
            }

            _logger.LogWarning("Secret '{SecretName}' not found in loaded secrets. Available secrets: {LoadedCount}", 
                name, _secrets.Count);
            return null;
        }
        
        /// <summary>
        /// Gets the number of secrets currently loaded in the provider.
        /// </summary>
        /// <returns>The count of loaded secrets.</returns>
        public int GetLoadedSecretsCount()
        {
            return _secrets.Count;
        }
        
        /// <summary>
        /// Determines whether a secret with the specified name is loaded.
        /// </summary>
        /// <param name="name">The name of the secret to check.</param>
        /// <returns><c>true</c> if the secret is loaded; otherwise, <c>false</c>.</returns>
        public bool HasSecret(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && _secrets.ContainsKey(name);
        }
        
        /// <summary>
        /// Gets the names of all secrets currently loaded in the provider.
        /// </summary>
        /// <returns>An enumerable of loaded secret names.</returns>
        public IEnumerable<string> GetLoadedSecretNames()
        {
            return _secrets.Keys.ToList();
        }
        
        private async Task LoadSingleSecretAsync(string secretName)
        {
            // Use relative URL - BaseAddress is configured in constructor
            using var response = await _httpClient.GetAsync(secretName);
           
            // Handle 404 as acceptable - secret doesn't exist
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Secret '{SecretName}' not found in store '{SecretStore}' - this is acceptable", secretName, _settings.SecretStoreName);
                return;
            }
           
            // Ensure success status for other status codes
            response.EnsureSuccessStatusCode();
           
            var responseContent = await response.Content.ReadAsStringAsync();
           
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("Empty response received for secret '{SecretName}'", secretName);
                return;
            }


            try
            {
                ParseAndStoreSecret(secretName, responseContent);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize secret '{SecretName}' response: {ResponseContent}", secretName, responseContent);
                throw; // Re-throw to trigger retry logic
            }
        }
        
        private void ParseAndStoreSecret(string secretName, string responseContent)
        {
            // Dapr returns JSON: { "secretName": "value" }
            var secretData = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
            
            if (secretData == null)
            {
                _logger.LogWarning("Secret '{SecretName}' found but response was null after deserialization", secretName);
                return;
            }
            
            if (!secretData.TryGetValue(secretName, out var secretValue))
            {
                _logger.LogWarning("Secret '{SecretName}' not found in response dictionary", secretName);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(secretValue))
            {
                _logger.LogWarning("Secret '{SecretName}' found but contains empty or whitespace-only value", secretName);
                return;
            }
            
            _secrets[secretName] = secretValue;
            _logger.LogDebug("Secret '{SecretName}' loaded successfully", secretName);
        }
        
        private static bool IsTransientError(HttpRequestException ex)
        {
            // Check for common transient HTTP errors in message (network/timeout issues)
            return ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase);
        }
        
        private async Task LoadSingleSecretWithRetryAsync(string secretName)
        {
            const int maxRetries = 3;
            var baseDelay = TimeSpan.FromMilliseconds(500);
           
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await LoadSingleSecretAsync(secretName);
                    _logger.LogDebug("Successfully loaded secret: {SecretName}", secretName);
                    return; // Success, exit retry loop
                }
                catch (HttpRequestException ex) when (IsTransientError(ex) && attempt < maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                    _logger.LogWarning("Transient error loading secret '{SecretName}' (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms. Error: {Error}", secretName, attempt + 1, maxRetries + 1, delay.TotalMilliseconds, ex.Message);
                   
                    await Task.Delay(delay);
                }
                catch (HttpRequestException ex) when (IsTransientError(ex))
                {
                    // Transient error on final attempt - let loop exit naturally
                    _logger.LogError(ex, "Transient error on final attempt loading secret '{SecretName}'. Max retries exhausted.", secretName);
                }
                catch (Exception ex)
                {
                    // Non-transient error - stop immediately
                    _logger.LogError(ex, "Non-retryable error loading secret '{SecretName}' (attempt {Attempt}).", secretName, attempt + 1);
                    break;
                }
            }
        }
    }
}
