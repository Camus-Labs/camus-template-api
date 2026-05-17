using System.Net.Http.Json;
using emc.camus.application.Common;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// General-purpose <see cref="HttpClient"/> extension methods for integration tests.
/// </summary>
public static class HttpClientExtensions
{
    private static int _idempotencyCounter;

    /// <summary>
    /// Replaces the default X-Forwarded-For header with the specified IP address.
    /// Returns the same client for fluent chaining.
    /// </summary>
    /// <param name="client">The HTTP client to configure.</param>
    /// <param name="ipAddress">The IP address to set in the X-Forwarded-For header.</param>
    /// <returns>The same <see cref="HttpClient"/> for fluent chaining.</returns>
    public static HttpClient WithIp(this HttpClient client, string ipAddress)
    {
        client.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);
        return client;
    }

    /// <summary>
    /// Sends a POST request with a unique Idempotency-Key header and JSON body.
    /// Used for endpoints decorated with [RequireIdempotencyKey].
    /// </summary>
    public static async Task<HttpResponseMessage> PostAsJsonWithIdempotencyKeyAsync<T>(
        this HttpClient client,
        string requestUri,
        T value,
        CancellationToken ct = default)
    {
        var key = $"test-idempotency-{Interlocked.Increment(ref _idempotencyCounter)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(value)
        };
        request.Headers.TryAddWithoutValidation(Headers.IdempotencyKey, key);

        return await client.SendAsync(request, ct);
    }
}
