using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.api.Models.Dtos.V2;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;
using FluentAssertions;

namespace emc.camus.api.integration.test.Auth;

[Trait("Category", "Integration")]
[Collection(PostgreSqlTestGroup.Name)]
public class AuthPostgreSqlEndpointTests : IAsyncLifetime
{
    private readonly ApiPostgreSqlFactory _factory;

    public AuthPostgreSqlEndpointTests(ApiPostgreSqlFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    public async ValueTask InitializeAsync() => await _factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task GenerateToken_ValidRequest_ReturnsOkWithPersistedToken()
    {
        // Arrange
        var client = await _factory.AuthenticateAsync("Admin", "adminsecret");
        var request = new
        {
            UsernameSuffix = "inttest",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", request, TestContext.Current.CancellationToken);

        // Assert — HTTP response
        await response.Should().HaveStatusCode(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GenerateTokenResponse>>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Token.Should().NotBeNullOrWhiteSpace();
        body.Data.ExpiresOn.Should().BeAfter(DateTime.UtcNow);
        body.Data.TokenUsername.Should().Be("Admin-inttest");

        // Assert — database row persisted
        var jti = AuthenticatedClientHelper.ExtractJti(body.Data.Token);
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        var row = await connection.QuerySingleAsync<dynamic>(
            "SELECT creator_user_id, creator_username, token_username, is_revoked, permissions FROM camus.generated_tokens WHERE jti = @Jti",
            new { Jti = jti });

        Guid creatorUserId = row.creator_user_id;
        string creatorUsername = row.creator_username;
        string tokenUsername = row.token_username;
        bool isRevoked = row.is_revoked;
        string[] permissions = row.permissions;

        creatorUserId.Should().NotBeEmpty();
        creatorUsername.Should().Be("Admin");
        tokenUsername.Should().Be("Admin-inttest");
        isRevoked.Should().BeFalse();
        permissions.Should().Contain("api.read");

        // Assert — audit record persisted
        var audit = await connection.QuerySingleAsync<dynamic>(
            "SELECT user_name, action_title FROM camus.action_audit WHERE action_title = 'token.generate.success' ORDER BY created_at DESC LIMIT 1");

        ((string)audit.user_name).Should().Be("Admin");
        ((string)audit.action_title).Should().Be("token.generate.success");
    }

    [Fact]
    public async Task Authenticate_ClientApp_UpdatesLastLoginAndAuditFields()
    {
        // Arrange — snapshot the user row before authentication
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var userBefore = await connection.QuerySingleAsync<dynamic>(
            "SELECT created_by, updated_by, last_login FROM camus.users WHERE username = 'ClientApp'");

        ((string)userBefore.created_by).Should().Be("Admin");
        ((string)userBefore.updated_by).Should().Be("Admin");
        ((DateTime?)userBefore.last_login).Should().BeNull();

        // Act — authenticate as ClientApp
        var before = DateTime.UtcNow;
        await _factory.AuthenticateAsync("ClientApp", "clientsecret");
        var after = DateTime.UtcNow;

        // Assert — user row updated with last_login and audit fields
        var userAfter = await connection.QuerySingleAsync<dynamic>(
            "SELECT created_by, updated_by, last_login FROM camus.users WHERE username = 'ClientApp'");

        ((string)userAfter.created_by).Should().Be("Admin", "seed creator must be preserved");
        ((string)userAfter.updated_by).Should().Be("ApiKeyUser", "trigger sets updated_by to the HTTP identity (API key)");
        ((DateTime?)userAfter.last_login).Should().NotBeNull();
        ((DateTime)userAfter.last_login).Should().BeOnOrAfter(before).And.BeOnOrBefore(after);

        // Assert — audit record persisted
        var audit = await connection.QuerySingleAsync<dynamic>(
            "SELECT user_name, action_title FROM camus.action_audit WHERE action_title = 'user.login.success' AND user_name = 'ClientApp' ORDER BY created_at DESC LIMIT 1");

        ((string)audit.user_name).Should().Be("ClientApp");
        ((string)audit.action_title).Should().Be("user.login.success");
    }

    [Fact]
    public async Task RevokeToken_ValidJti_ReturnsOkWithRevokedToken()
    {
        // Arrange — generate a token first
        var client = await _factory.AuthenticateAsync("Admin", "adminsecret");
        var generateRequest = new
        {
            UsernameSuffix = "revoke",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        var generateResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", generateRequest, TestContext.Current.CancellationToken);
        await generateResponse.Should().HaveStatusCode(HttpStatusCode.Created, "token generation must succeed for test setup");

        var generateBody = await generateResponse.Content.ReadFromJsonAsync<ApiResponse<GenerateTokenResponse>>(TestContext.Current.CancellationToken);
        var jti = AuthenticatedClientHelper.ExtractJti(generateBody!.Data!.Token);

        // Act
        var response = await client.PostAsync($"/api/v2.0/auth/tokens/{jti}/revoke", null, TestContext.Current.CancellationToken);

        // Assert — HTTP response
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GeneratedTokenSummaryDto>>(TestContext.Current.CancellationToken);
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
        var client = await _factory.AuthenticateAsync("Admin", "adminsecret");

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

        var firstResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", firstRequest, TestContext.Current.CancellationToken);
        await firstResponse.Should().HaveStatusCode(HttpStatusCode.Created, "first token generation must succeed for test setup");

        var secondResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", secondRequest, TestContext.Current.CancellationToken);
        await secondResponse.Should().HaveStatusCode(HttpStatusCode.Created, "second token generation must succeed for test setup");

        // Act
        var response = await client.GetAsync("/api/v2.0/auth/tokens?Page=1&PageSize=50", TestContext.Current.CancellationToken);

        // Assert — HTTP response
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().Contain(t => t.TokenUsername == "Admin-list-a");
        body.Data.Items.Should().Contain(t => t.TokenUsername == "Admin-list-b");
        body.Data.TotalCount.Should().Be(2);

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
        var client = await _factory.AuthenticateAsync("Admin", "adminsecret");
        var firstRequest = new
        {
            UsernameSuffix = "duplicate",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", firstRequest, TestContext.Current.CancellationToken);
        await firstResponse.Should().HaveStatusCode(HttpStatusCode.Created, "first token generation must succeed for test setup");

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

        var response = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", duplicateRequest, TestContext.Current.CancellationToken);

        // Assert — request rejected with Conflict and correct error code
        await response.Should().HaveStatusCode(HttpStatusCode.Conflict);
        await response.Should().HaveErrorCode("data_conflict");

        // Assert — no additional token or audit record was persisted (transaction rolled back)
        var tokenCountAfter = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.generated_tokens");
        var auditCountAfter = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM camus.action_audit WHERE action_title = 'token.generate.success'");

        tokenCountAfter.Should().Be(tokenCountBefore);
        auditCountAfter.Should().Be(auditCountBefore);
    }

    [Fact]
    public async Task RevokedToken_UsedOnProtectedEndpoint_ReturnsUnauthorized()
    {
        // Arrange — authenticate, generate a token, then revoke it
        var adminClient = await _factory.AuthenticateAsync("Admin", "adminsecret");
        var generateRequest = new
        {
            UsernameSuffix = "revoked-use",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        var generateResponse = await adminClient.PostAsJsonAsync("/api/v2.0/auth/generate-token", generateRequest, TestContext.Current.CancellationToken);
        await generateResponse.Should().HaveStatusCode(HttpStatusCode.Created, "token generation must succeed for test setup");

        var generateBody = await generateResponse.Content.ReadFromJsonAsync<ApiResponse<GenerateTokenResponse>>(TestContext.Current.CancellationToken);
        var generatedToken = generateBody!.Data!.Token;
        var jti = AuthenticatedClientHelper.ExtractJti(generatedToken);

        // Assert — the generated token works on a JWT-protected endpoint before revocation
        var tokenClient = _factory.CreateClient();
        tokenClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", generatedToken);

        var preRevokeResponse = await tokenClient.GetAsync("/api/v2.0/apiinfo/info-jwt", TestContext.Current.CancellationToken);
        await preRevokeResponse.Should().HaveStatusCode(HttpStatusCode.OK, "generated token must be accepted before revocation");

        // Act — revoke the token and use it again
        var revokeResponse = await adminClient.PostAsync($"/api/v2.0/auth/tokens/{jti}/revoke", null, TestContext.Current.CancellationToken);
        await revokeResponse.Should().HaveStatusCode(HttpStatusCode.OK, "token revocation must succeed for test setup");

        var postRevokeResponse = await tokenClient.GetAsync("/api/v2.0/apiinfo/info-jwt", TestContext.Current.CancellationToken);

        // Assert — the revocation cache rejects the token via OnTokenValidated
        await postRevokeResponse.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        await postRevokeResponse.Should().HaveErrorCode("jwt_token_revoked");
    }

    [Fact]
    public async Task ExpiredToken_UsedOnProtectedEndpoint_ReturnsUnauthorizedWithExpiredCode()
    {
        // Arrange — create a JWT that expires almost immediately
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var signingCredentials = scope.ServiceProvider.GetRequiredService<SigningCredentials>();

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
            expires: DateTime.UtcNow.AddMilliseconds(50),
            signingCredentials: signingCredentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        SpinWait.SpinUntil(() => DateTime.UtcNow > token.ValidTo);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenString);

        // Act
        var response = await client.GetAsync("/api/v2.0/apiinfo/info-jwt", TestContext.Current.CancellationToken);

        // Assert — framework rejects the expired token before OnTokenValidated fires
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        await response.Should().HaveErrorCode("jwt_token_expired");
    }

}
