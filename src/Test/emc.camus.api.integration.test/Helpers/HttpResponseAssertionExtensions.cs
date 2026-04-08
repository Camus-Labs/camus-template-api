using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// FluentAssertions extensions for <see cref="HttpResponseMessage"/> that include the response
/// body in failure messages. Eliminates the need to manually inspect server logs when a status
/// code assertion fails — the error details (e.g., ProblemDetails JSON) appear directly in the
/// test output.
/// </summary>
public static class HttpResponseAssertionExtensions
{
    /// <summary>
    /// Asserts that the response has the expected status code. On failure, reads and includes
    /// the response body in the assertion message for immediate diagnostics.
    /// </summary>
    public static async Task HaveStatusCode(
        this ObjectAssertions assertions,
        HttpStatusCode expected,
        string because = "",
        params object[] becauseArgs)
    {
        var response = (HttpResponseMessage)assertions.Subject;
        var actual = response.StatusCode;

        if (actual != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            var truncatedBody = body.Length > 2000 ? body[..2000] + "... (truncated)" : body;

            actual.Should().Be(
                expected,
                $"{because}{(string.IsNullOrEmpty(because) ? "" : " — ")}Response body: {truncatedBody}",
                becauseArgs);
        }
    }

    /// <summary>
    /// Asserts that the response body contains a ProblemDetails <c>error</c> extension with the
    /// expected machine-readable error code. On failure, includes the full response body in the
    /// assertion message for immediate diagnostics.
    /// </summary>
    public static async Task HaveErrorCode(
        this ObjectAssertions assertions,
        string expectedErrorCode,
        string because = "",
        params object[] becauseArgs)
    {
        var response = (HttpResponseMessage)assertions.Subject;
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.TryGetProperty("error", out var errorProperty).Should().BeTrue(
            $"response body should contain an 'error' property but was: {body}");

        errorProperty.GetString().Should().Be(
            expectedErrorCode,
            because,
            becauseArgs);
    }
}
