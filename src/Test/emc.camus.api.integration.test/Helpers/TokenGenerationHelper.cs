using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Helper methods for generating multiple tokens in integration test arrange steps.
/// Extracts loop logic from test methods to keep them linear AAA sequences.
/// </summary>
public static class TokenGenerationHelper
{
    /// <summary>
    /// Generates tokens with the specified username suffixes via the generate-token endpoint.
    /// Asserts each generation returns <see cref="HttpStatusCode.Created"/>.
    /// </summary>
    public static async Task GenerateTokensAsync(
        HttpClient client,
        string[] suffixes,
        CancellationToken ct)
    {
        foreach (var suffix in suffixes)
        {
            var request = new
            {
                UsernameSuffix = suffix,
                ExpiresOn = DateTime.UtcNow.AddHours(2),
                Permissions = new[] { "api.read" },
            };

            var response = await client.PostAsJsonAsync("/api/v2/auth/generate-token", request, ct);
            await response.Should().HaveStatusCode(HttpStatusCode.Created, $"token generation for '{suffix}' must succeed for test setup");
        }
    }
}
