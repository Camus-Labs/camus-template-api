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
    /// Sends <paramref name="permitLimit"/> requests to the specified endpoint,
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
    /// Sends <paramref name="permitLimit"/> GET requests to the specified endpoint
    /// without asserting response status. Used to exhaust permits regardless of individual outcomes.
    /// </summary>
    public static async Task ExhaustRateLimitAsync(
        HttpClient client,
        string endpoint,
        int permitLimit,
        CancellationToken ct)
    {
        for (var i = 0; i < permitLimit; i++)
        {
            await client.GetAsync(endpoint, ct);
        }
    }

    /// <summary>
    /// Sends <paramref name="permitLimit"/> POST requests to the specified endpoint
    /// without asserting response status. Used to exhaust permits for POST-based endpoints.
    /// </summary>
    public static async Task ExhaustRateLimitPostAsync(
        HttpClient client,
        string endpoint,
        int permitLimit,
        CancellationToken ct)
    {
        for (var i = 0; i < permitLimit; i++)
        {
            await client.PostAsync(endpoint, null, ct);
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
