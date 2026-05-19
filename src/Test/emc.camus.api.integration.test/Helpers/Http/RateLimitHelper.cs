using System.Net;
using FluentAssertions;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Helper methods for exhausting rate limit permits in integration tests.
/// Extracts repetitive loop logic from test methods to keep them linear AAA sequences.
/// </summary>
public static class RateLimitHelper
{
    /// <summary>
    /// Sends <paramref name="permitLimit"/> GET requests to the specified endpoint,
    /// asserting each responds with <see cref="HttpStatusCode.OK"/>.
    /// </summary>
    public static async Task ExhaustRateLimitWithAssertAsync(
        HttpClient client,
        string endpoint,
        int permitLimit,
        CancellationToken ct)
    {
        for (var i = 0; i < permitLimit; i++)
        {
            var response = await client.GetAsync(endpoint, ct);
            await response.Should().HaveStatusCode(HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// Sends <paramref name="permitLimit"/> requests to the specified endpoint
    /// without asserting response status. Uses GET by default; pass <see cref="HttpMethod.Post"/>
    /// for POST-based endpoints.
    /// </summary>
    public static async Task ExhaustRateLimitAsync(
        HttpClient client,
        string endpoint,
        int permitLimit,
        CancellationToken ct,
        HttpMethod? method = null)
    {
        method ??= HttpMethod.Get;

        for (var i = 0; i < permitLimit; i++)
        {
            using var request = new HttpRequestMessage(method, endpoint);
            await client.SendAsync(request, ct);
        }
    }

    /// <summary>
    /// Exhausts the rate limit and verifies the next request is rejected with 429.
    /// Throws <see cref="InvalidOperationException"/> if the limit is not exhausted.
    /// Use in test Arrange sections to avoid FluentAssertions calls before Act.
    /// </summary>
    public static async Task ExhaustAndVerifyRateLimitAsync(
        HttpClient client,
        string endpoint,
        int permitLimit,
        CancellationToken ct)
    {
        await ExhaustRateLimitAsync(client, endpoint, permitLimit, ct);

        var rejectedResponse = await client.GetAsync(endpoint, ct);
        if (rejectedResponse.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException(
                $"Rate limit not exhausted after {permitLimit} requests: expected 429 but got {(int)rejectedResponse.StatusCode}");
        }
    }

    /// <summary>
    /// Sends <paramref name="permitLimit"/> requests using two clients to the same endpoint,
    /// exhausting both clients' rate limits simultaneously.
    /// </summary>
    public static async Task ExhaustRateLimitForBothAsync(
        HttpClient clientA,
        HttpClient clientB,
        string endpoint,
        int permitLimit,
        CancellationToken ct)
    {
        for (var i = 0; i < permitLimit; i++)
        {
            await clientA.GetAsync(endpoint, ct);
            await clientB.GetAsync(endpoint, ct);
        }
    }
}
