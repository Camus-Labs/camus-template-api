using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using emc.camus.application.Secrets;
using emc.camus.secrets.dapr.Configurations;

namespace emc.camus.secrets.dapr.Services
{
    /// <summary>
    /// Provides secret management using Dapr's secret store API.
    /// </summary>
    /// <remarks>
    /// This implementation loads secrets from a Dapr sidecar using HTTP requests and caches them in memory.
    /// </remarks>
    internal sealed class DaprSecretProvider : ISecretProvider
    {
        private readonly HttpClient _httpClient;
        private readonly DaprSecretProviderSettings _settings;
        private readonly ConcurrentDictionary<string, string> _secrets = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DaprSecretProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to communicate with the Dapr sidecar.</param>
        /// <param name="settings">The configuration settings for the Dapr secret provider.</param>
        public DaprSecretProvider(HttpClient httpClient, DaprSecretProviderSettings settings)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(settings);

            _httpClient = httpClient;
            _settings = settings;

            // Configure HttpClient with adapter-specific settings
            _httpClient.BaseAddress = new Uri($"http://{_settings.BaseHost}:{_settings.HttpPort}/v1.0/secrets/{_settings.SecretStoreName}/");
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        /// <summary>
        /// Loads the specified secrets from the Dapr secret store asynchronously.
        /// </summary>
        /// <param name="secretNames">A collection of secret names to load.</param>
        /// <returns>A task representing the asynchronous load operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="secretNames"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when any secret name is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a secret is not found, empty, or cannot be parsed.</exception>
        public async Task LoadSecretsAsync(IEnumerable<string> secretNames)
        {
            ArgumentNullException.ThrowIfNull(secretNames);

            var secretNamesList = secretNames.ToList();

            if (secretNamesList.Count == 0)
            {
                return;
            }

            if (secretNamesList.Any(s => string.IsNullOrWhiteSpace(s)))
            {
                throw new ArgumentException("Secret names cannot contain null or whitespace entries.", nameof(secretNames));
            }

            foreach (var secretName in secretNamesList)
            {
                await LoadSingleSecretAsync(secretName);
            }
        }

        /// <summary>
        /// Retrieves the value of a loaded secret by name.
        /// </summary>
        /// <param name="name">The name of the secret to retrieve.</param>
        /// <returns>The secret value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the secret is not found in the cache.</exception>
        public string GetSecret(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (_secrets.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new InvalidOperationException($"Secret '{name}' not found in loaded secrets. Ensure it was included in the list of secrets to load and that it exists in the Dapr secret store.");
        }

        /// <summary>
        /// Loads a single secret from the Dapr secret store via HTTP.
        /// </summary>
        /// <param name="secretName">The name of the secret to load.</param>
        /// <returns>A task representing the asynchronous load operation.</returns>
        /// <remarks>
        /// Throws on 404 (secret not found), empty responses, and non-success HTTP status codes.
        /// </remarks>
        private async Task LoadSingleSecretAsync(string secretName)
        {
            try
            {
                using var response = await _httpClient.GetAsync(secretName);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException($"Secret '{secretName}' not found in store '{_settings.SecretStoreName}'");
                }

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    throw new InvalidOperationException($"Empty response received for secret '{secretName}'");
                }

                ParseAndStoreSecret(secretName, responseContent);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load secret '{secretName}' from store '{_settings.SecretStoreName}'", ex);
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


    }
}
