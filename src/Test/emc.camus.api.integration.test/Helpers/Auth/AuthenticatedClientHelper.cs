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
    private static readonly string[] ReadPermissions = ["api.read"];

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
        return factory.CreateJwtClient(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "test-user", permissions);
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with a valid JWT Bearer token using a specific user ID and username.
    /// Uses the registered <see cref="ITokenGenerator"/> from the test server's DI container.
    /// </summary>
    /// <param name="factory">The factory to create the client from.</param>
    /// <param name="userId">The user ID to include as the subject claim.</param>
    /// <param name="username">The username to include as the name claim.</param>
    /// <param name="permissions">Permissions to include as claims in the token.</param>
    /// <returns>An authenticated <see cref="HttpClient"/>.</returns>
    public static HttpClient CreateJwtClient(
        this Fixtures.ApiFactoryBase factory,
        Guid userId,
        string username,
        params string[] permissions)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();

        var permissionClaims = permissions
            .Select(p => new Claim(Permissions.ClaimType, p))
            .ToList();

        var authToken = tokenGenerator.GenerateToken(
            userId,
            username,
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

        var authResponse = await apiKeyClient.PostAsJsonWithIdempotencyKeyAsync("/api/v2/auth/authenticate", authRequest);
        await authResponse.Should().HaveStatusCode(HttpStatusCode.OK, $"authentication for '{username}' must succeed for test setup");

        var authBody = await authResponse.Content.ReadFromJsonAsync<ApiResponse<AuthenticateUserResponse>>();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authBody!.Data!.Token);

        return client;
    }

    /// <summary>
    /// Authenticates as the Admin seed user and returns an <see cref="HttpClient"/> with the resulting JWT.
    /// </summary>
    /// <param name="factory">The factory to create clients from.</param>
    /// <returns>An authenticated <see cref="HttpClient"/>.</returns>
    public static Task<HttpClient> AuthenticateAsAdminAsync(this Fixtures.ApiFactoryBase factory)
        => factory.AuthenticateAsync("Admin", "adminsecret");

    /// <summary>
    /// Authenticates as the ClientApp seed user and returns an <see cref="HttpClient"/> with the resulting JWT.
    /// </summary>
    /// <param name="factory">The factory to create clients from.</param>
    /// <returns>An authenticated <see cref="HttpClient"/>.</returns>
    public static Task<HttpClient> AuthenticateAsClientAppAsync(this Fixtures.ApiFactoryBase factory)
        => factory.AuthenticateAsync("ClientApp", "clientsecret");

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

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with an expired JWT Bearer token.
    /// The token is generated with real signing credentials and a near-zero lifetime,
    /// then the method waits for expiration before returning.
    /// </summary>
    /// <param name="factory">The factory to create the client from.</param>
    /// <returns>An <see cref="HttpClient"/> whose JWT has already expired.</returns>
    public static HttpClient CreateExpiredJwtClient(this Fixtures.ApiFactoryBase factory)
    {
        using var scope = factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var signingCredentials = scope.ServiceProvider.GetRequiredService<Microsoft.IdentityModel.Tokens.SigningCredentials>();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            new(JwtRegisteredClaimNames.UniqueName, "expired-test-user"),
            new(JwtRegisteredClaimNames.Jti, "ffffffff-ffff-ffff-ffff-ffffffffffff"),
        };

        var token = new JwtSecurityToken(
            issuer: config["JwtSettings:Issuer"],
            audience: config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddYears(-100),
            signingCredentials: signingCredentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        return client;
    }

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
                ExpiresOn = DateTime.UtcNow.AddYears(1).AddDays(-1),
                Permissions = ReadPermissions,
            };

            var response = await client.PostAsJsonWithIdempotencyKeyAsync("/api/v2/auth/generate-token", request, ct);
            await response.Should().HaveStatusCode(HttpStatusCode.Created, $"token generation for '{suffix}' must succeed for test setup");
        }
    }

}
