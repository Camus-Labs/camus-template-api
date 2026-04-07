using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Secrets;

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
    /// </summary>
    /// <param name="factory">The factory to create the client from.</param>
    /// <param name="permissions">Permissions to include as claims in the token.</param>
    /// <returns>An authenticated <see cref="HttpClient"/>.</returns>
    public static HttpClient CreateJwtClient(
        this Fixtures.CamusApiFactoryBase factory,
        params string[] permissions)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var signingCredentials = scope.ServiceProvider.GetRequiredService<SigningCredentials>();

        var issuer = config["JwtSettings:Issuer"];
        var audience = config["JwtSettings:Audience"];

        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, "test-user"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(Permissions.ClaimType, permission));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: signingCredentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenString);

        return client;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with a valid API Key in the Api-Key header.
    /// </summary>
    /// <param name="factory">The factory to create the client from.</param>
    /// <returns>An authenticated <see cref="HttpClient"/>.</returns>
    public static HttpClient CreateApiKeyClient(this Fixtures.CamusApiFactoryBase factory)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var secretProvider = scope.ServiceProvider.GetRequiredService<ISecretProvider>();
        var apiKey = secretProvider.GetSecret("XApiKey");

        client.DefaultRequestHeaders.Add(Headers.ApiKey, apiKey);

        return client;
    }
}
