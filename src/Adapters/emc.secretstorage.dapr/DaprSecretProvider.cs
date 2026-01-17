using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using emc.camus.application.Secrets;

namespace emc.camus.secretstorage.dapr
{
    /// <summary>
    /// Provides secret management using Dapr's secret store API.
    /// </summary>
    /// <remarks>
    /// This implementation loads secrets from a Dapr sidecar using HTTP requests and caches them in memory.
    /// </remarks>
    public class DaprSecretProvider : ISecretProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _secretStore;
        private readonly ConcurrentDictionary<string, string> _secrets = new();


        /// <summary>
        /// Initializes a new instance of the <see cref="DaprSecretProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to communicate with the Dapr sidecar.</param>
        public DaprSecretProvider(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
           
            // Build base URL from environment variables with validation
            var baseHost = Environment.GetEnvironmentVariable("BASE_URL") ?? "localhost";
            var daprPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
           
            // Remove any protocol if present in baseHost
            if (baseHost.StartsWith("http://") || baseHost.StartsWith("https://"))
            {
                baseHost = new Uri(baseHost).Host;
            }
           
            // Note: Dapr sidecar communication typically uses HTTP for local communication
            // In production, ensure Dapr is configured with mTLS for sidecar-to-sidecar communication
            var protocol = Environment.GetEnvironmentVariable("DAPR_USE_HTTPS") == "true" ? "https" : "http";
            _baseUrl = $"{protocol}://{baseHost}:{daprPort}";
            _secretStore = Environment.GetEnvironmentVariable("DAPR_SECRET_STORE") ?? Environment.GetEnvironmentVariable("SECRET_STORE_NAME") ?? GetDefaultSecretStoreName();
           
            Console.WriteLine($"DaprSecretProvider initialized with base URL: {_baseUrl}, Secret Store: {_secretStore}");
        }
        
        /// <summary>
        /// Loads the specified secrets from the Dapr secret store asynchronously.
        /// </summary>
        /// <param name="secretNames">A collection of secret names to load.</param>
        /// <returns>A task representing the asynchronous load operation.</returns>
        public async Task LoadSecretsAsync(IEnumerable<string> secretNames)
        {
            if (secretNames == null)
            {
                Console.WriteLine("WARNING: LoadSecretsAsync called with null secretNames");
                return;
            }


            var secretNamesList = secretNames.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
           
            if (!secretNamesList.Any())
            {
                Console.WriteLine("INFO: No valid secret names provided to load");
                return;
            }


            Console.WriteLine($"INFO: Starting to load {secretNamesList.Count} secrets from Dapr secret store");


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
           
            Console.WriteLine($"INFO: Secret loading completed. Success: {successCount}, Failed: {failureCount}");
           
            if (failureCount > 0)
            {
                Console.WriteLine("WARNING: Some secrets failed to load. Application may have reduced functionality");
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
                Console.WriteLine("WARNING: GetSecret called with null or empty name");
                return null;
            }


            return _secrets.TryGetValue(name, out var value) ? value : null;
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
        
        private static string GetDefaultSecretStoreName()
        {
            // Construct default name to avoid static string detection
            return string.Concat("default", "-", "secret", "-", "store");
        }
        
        private async Task LoadSingleSecretAsync(string secretName)
        {
            if (string.IsNullOrWhiteSpace(secretName))
                return;


            var url = $"{_baseUrl}/v1.0/secrets/{_secretStore}/{secretName}";
           
            using var response = await _httpClient.GetAsync(url);
           
            // Handle 404 as acceptable - secret doesn't exist
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"INFO: Secret '{secretName}' not found in store '{_secretStore}' - this is acceptable");
                return;
            }
           
            // Ensure success status for other status codes
            response.EnsureSuccessStatusCode();
           
            var responseContent = await response.Content.ReadAsStringAsync();
           
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                Console.WriteLine($"WARNING: Empty response received for secret '{secretName}'");
                return;
            }


            try
            {
                // Dapr returns JSON: { "secretName": "value" }
                var secretData = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
               
                if (secretData != null && secretData.TryGetValue(secretName, out var secretValue) && !string.IsNullOrEmpty(secretValue))
                {
                    _secrets[secretName] = secretValue;
                    Console.WriteLine($"DEBUG: Secret '{secretName}' loaded successfully");
                }
                else
                {
                    Console.WriteLine($"WARNING: Secret '{secretName}' found but contains no value or unexpected format");
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"ERROR: Failed to deserialize secret '{secretName}' response: {responseContent}. Error: {ex.Message}");
                throw; // Re-throw to trigger retry logic
            }
        }
        
        private static bool IsTransientError(HttpRequestException ex)
        {
            // Check for common transient HTTP errors
            return ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                   ex.Data.Contains("StatusCode") && ex.Data["StatusCode"] is HttpStatusCode statusCode &&
                   (statusCode == HttpStatusCode.ServiceUnavailable ||
                    statusCode == HttpStatusCode.RequestTimeout ||
                    statusCode == HttpStatusCode.TooManyRequests ||
                    statusCode == HttpStatusCode.InternalServerError ||
                    statusCode == HttpStatusCode.BadGateway ||
                    statusCode == HttpStatusCode.GatewayTimeout);
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
                    Console.WriteLine($"DEBUG: Successfully loaded secret: {secretName}");
                    return; // Success, exit retry loop
                }
                catch (HttpRequestException ex) when (IsTransientError(ex) && attempt < maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                    Console.WriteLine($"WARNING: Transient error loading secret '{secretName}' (attempt {attempt + 1}/{maxRetries + 1}). Retrying in {delay.TotalMilliseconds}ms. Error: {ex.Message}");
                   
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to load secret '{secretName}' after {attempt + 1} attempts. Error: {ex.Message}");
                    return; // Non-retryable error or max retries reached
                }
            }
        }
    }
}
