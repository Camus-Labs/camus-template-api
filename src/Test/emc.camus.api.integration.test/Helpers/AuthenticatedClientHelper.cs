using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Secrets;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;
using FluentAssertions;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Provides factory methods for creating pre-authenticated <see cref="HttpClient"/> instances
/// for integration tests. Resolves signing keys and secrets from the test server's DI container
/// so tokens and API keys are valid against the running pipeline.
/// </summary>
public static class AuthenticatedClientHelper
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> with a valid JWT Bearer token in the Authorization header.
    /// Uses the registered <see cref="ITokenGenerator"/> from the test server's DI container.
    /// </summary>
    /// <param name="factory">The factory to create the client from.</param>
    /// <param name="permissions">Permissions to include as claims in the token.</param>
    /// <returns>An authenticated <see cref="HttpClient"/>.</returns>
    public static HttpClient CreateJwtClient(
        this Fixtures.ApiFactoryBase factory,
        params string[] permissions)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();

        var permissionClaims = permissions
            .Select(p => new Claim(Permissions.ClaimType, p))
            .ToList();

        var authToken = tokenGenerator.GenerateToken(
            Guid.NewGuid(),
            "test-user",
            additionalClaims: permissionClaims);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authToken.Token);

        return client;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with a valid API Key in the Api-Key header.
    /// </summary>
    /// <param name="factory">The factory to create the client from.</param>
    /// <returns>An authenticated <see cref="HttpClient"/>.</returns>
    public static HttpClient CreateApiKeyClient(this Fixtures.ApiFactoryBase factory)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var secretProvider = scope.ServiceProvider.GetRequiredService<ISecretProvider>();
        var apiKey = secretProvider.GetSecret("XApiKey");

        client.DefaultRequestHeaders.Add(Headers.ApiKey, apiKey);

        return client;
    }

    /// <summary>
    /// Authenticates against the real auth endpoint using migration-seeded credentials and
    /// returns an <see cref="HttpClient"/> with the resulting JWT Bearer token.
    /// Use for PostgreSQL integration tests where users come from database seed data.
    /// </summary>
    /// <param name="factory">The PS factory to create clients from.</param>
    /// <param name="username">The username to authenticate with.</param>
    /// <param name="password">The password to authenticate with.</param>
    /// <returns>An authenticated <see cref="HttpClient"/>.</returns>
    public static async Task<HttpClient> AuthenticateAsync(
        this Fixtures.ApiFactoryBase factory,
        string username,
        string password)
    {
        var apiKeyClient = factory.CreateApiKeyClient();
        var authRequest = new { Username = username, Password = password };

        var authResponse = await apiKeyClient.PostAsJsonAsync("/api/v2.0/auth/authenticate", authRequest);
        await authResponse.Should().HaveStatusCode(HttpStatusCode.OK, $"authentication for '{username}' must succeed for test setup");

        var authBody = await authResponse.Content.ReadFromJsonAsync<ApiResponse<AuthenticateUserResponse>>();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authBody!.Data!.Token);

        return client;
    }

    /// <summary>
    /// Extracts the JTI (JWT ID) claim from a raw JWT token string.
    /// </summary>
    /// <param name="token">The raw JWT token string.</param>
    /// <returns>The JTI as a <see cref="Guid"/>.</returns>
    public static Guid ExtractJti(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var jti = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        return Guid.Parse(jti);
    }
}
