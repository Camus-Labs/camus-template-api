using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Npgsql;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.api.Models.Dtos.V2;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;
using FluentAssertions;
using Xunit.Abstractions;

namespace emc.camus.api.integration.test.Auth;

[Trait("Category", "Integration")]
public class AuthPSEndpointTests : IClassFixture<CamusApiPSFactory>
{
    private readonly CamusApiPSFactory _factory;

    public AuthPSEndpointTests(CamusApiPSFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    [Fact]
    public async Task GenerateToken_ValidRequest_ReturnsOkWithPersistedToken()
    {
        // Arrange
        var client = await AuthenticateAdminAsync();
        var request = new
        {
            UsernameSuffix = "inttest",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", request);

        // Assert — HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GenerateTokenResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Token.Should().NotBeNullOrWhiteSpace();
        body.Data.ExpiresOn.Should().BeAfter(DateTime.UtcNow);
        body.Data.TokenUsername.Should().Be("Admin-inttest");

        // Assert — database row persisted
        var jti = ExtractJti(body.Data.Token);
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        var row = await connection.QuerySingleAsync<dynamic>(
            "SELECT creator_user_id, creator_username, token_username, is_revoked, permissions FROM camus.generated_tokens WHERE jti = @Jti",
            new { Jti = jti });

        ((Guid)row.creator_user_id).Should().NotBeEmpty();
        ((string)row.creator_username).Should().Be("Admin");
        ((string)row.token_username).Should().Be("Admin-inttest");
        ((bool)row.is_revoked).Should().BeFalse();
        ((string[])row.permissions).Should().Contain("api.read");

        // Assert — audit record persisted
        var audit = await connection.QuerySingleAsync<dynamic>(
            "SELECT user_name, action_title FROM camus.action_audit WHERE action_title = 'token.generate.success' ORDER BY created_at DESC LIMIT 1");

        ((string)audit.user_name).Should().Be("Admin");
        ((string)audit.action_title).Should().Be("token.generate.success");
    }

    [Fact]
    public async Task RevokeToken_ValidJti_ReturnsOkWithRevokedToken()
    {
        // Arrange — generate a token first
        var client = await AuthenticateAdminAsync();
        var generateRequest = new
        {
            UsernameSuffix = "revoke",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        var generateResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", generateRequest);
        generateResponse.StatusCode.Should().Be(HttpStatusCode.OK, "token generation must succeed for test setup");

        var generateBody = await generateResponse.Content.ReadFromJsonAsync<ApiResponse<GenerateTokenResponse>>();
        var jti = ExtractJti(generateBody!.Data!.Token);

        // Act
        var response = await client.PostAsync($"/api/v2.0/auth/tokens/{jti}/revoke", null);

        // Assert — HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GeneratedTokenSummaryDto>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.IsRevoked.Should().BeTrue();
        body.Data.RevokedAt.Should().NotBeNull();

        // Assert — database row updated
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        var row = await connection.QuerySingleAsync<dynamic>(
            "SELECT creator_user_id, creator_username, is_revoked, revoked_at FROM camus.generated_tokens WHERE jti = @Jti",
            new { Jti = jti });

        ((Guid)row.creator_user_id).Should().NotBeEmpty();
        ((string)row.creator_username).Should().Be("Admin");
        ((bool)row.is_revoked).Should().BeTrue();
        ((DateTime?)row.revoked_at).Should().NotBeNull();

        // Assert — audit record persisted
        var audit = await connection.QuerySingleAsync<dynamic>(
            "SELECT user_name, action_title FROM camus.action_audit WHERE action_title = 'token.revoke.success' ORDER BY created_at DESC LIMIT 1");

        ((string)audit.user_name).Should().Be("Admin");
        ((string)audit.action_title).Should().Be("token.revoke.success");
    }

    [Fact]
    public async Task GetTokens_AfterGenerating_ReturnsPagedTokensFromDatabase()
    {
        // Arrange — generate two tokens with distinct suffixes
        var client = await AuthenticateAdminAsync();

        var firstRequest = new
        {
            UsernameSuffix = "list-a",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };
        var secondRequest = new
        {
            UsernameSuffix = "list-b",
            ExpiresOn = DateTime.UtcNow.AddHours(3),
            Permissions = new[] { "api.read" },
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK, "first token generation must succeed for test setup");

        var secondResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", secondRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK, "second token generation must succeed for test setup");

        // Act
        var response = await client.GetAsync("/api/v2.0/auth/tokens?Page=1&PageSize=50");

        // Assert — HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().Contain(t => t.TokenUsername == "Admin-list-a");
        body.Data.Items.Should().Contain(t => t.TokenUsername == "Admin-list-b");
        body.Data.TotalCount.Should().BeGreaterThanOrEqualTo(2);

        // Assert — database rows match response count
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        var dbCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.generated_tokens WHERE token_username IN ('Admin-list-a', 'Admin-list-b')");

        dbCount.Should().Be(2);
    }

    [Fact]
    public async Task GenerateToken_DuplicateTokenUsername_ReturnsConflictAndNothingPersisted()
    {
        // Arrange — generate a token with a specific suffix first
        var client = await AuthenticateAdminAsync();
        var firstRequest = new
        {
            UsernameSuffix = "duplicate",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK, "first token generation must succeed for test setup");

        // Snapshot counts after the first successful insert
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        var tokenCountBefore = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.generated_tokens");
        var auditCountBefore = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.action_audit WHERE action_title = 'token.generate.success'");

        // Act — attempt to generate a token with the same suffix (same token_username)
        var duplicateRequest = new
        {
            UsernameSuffix = "duplicate",
            ExpiresOn = DateTime.UtcNow.AddHours(3),
            Permissions = new[] { "api.read" },
        };

        var response = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", duplicateRequest);

        // Assert — request rejected with Conflict and correct error code
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorBody = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorBody.GetProperty("error").GetString().Should().Be("data_conflict");

        // Assert — no additional token or audit record was persisted (transaction rolled back)
        var tokenCountAfter = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.generated_tokens");
        var auditCountAfter = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.action_audit WHERE action_title = 'token.generate.success'");

        tokenCountAfter.Should().Be(tokenCountBefore);
        auditCountAfter.Should().Be(auditCountBefore);
    }

    private async Task<HttpClient> AuthenticateAdminAsync()
    {
        var apiKeyClient = _factory.CreateApiKeyClient();

        // PS credentials come from migration seed data (001_initial_schema.sql),
        // not from StubSecretProvider which is used by the InMemory user repository.
        var authRequest = new { Username = "Admin", Password = "adminsecret" };

        var authResponse = await apiKeyClient.PostAsJsonAsync("/api/v2.0/auth/authenticate", authRequest);
        authResponse.StatusCode.Should().Be(HttpStatusCode.OK, "admin authentication must succeed for test setup");

        var authBody = await authResponse.Content.ReadFromJsonAsync<ApiResponse<AuthenticateUserResponse>>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authBody!.Data!.Token);

        return client;
    }

    private static Guid ExtractJti(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var jti = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        return Guid.Parse(jti);
    }
}
