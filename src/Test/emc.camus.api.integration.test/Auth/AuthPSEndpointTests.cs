using System.Net;
using System.Net.Http.Json;
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
[Collection(PostgreSqlTestGroup.Name)]
public class AuthPSEndpointTests : IAsyncLifetime
{
    private readonly ApiPSFactory _factory;

    public AuthPSEndpointTests(ApiPSFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

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
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GenerateTokenResponse>>();
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
    public async Task Authenticate_ClientApp_UpdatesLastLoginAndAuditFields()
    {
        // Arrange — snapshot the user row before authentication
        var before = DateTime.UtcNow;
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        var userBefore = await connection.QuerySingleAsync<dynamic>(
            "SELECT created_by, updated_by, last_login FROM camus.users WHERE username = 'ClientApp'");

        ((string)userBefore.created_by).Should().Be("Admin");
        ((string)userBefore.updated_by).Should().Be("Admin");
        ((DateTime?)userBefore.last_login).Should().BeNull();

        // Act — authenticate as ClientApp
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
        var client = await AuthenticateAdminAsync();
        var generateRequest = new
        {
            UsernameSuffix = "revoke",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        var generateResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", generateRequest);
        await generateResponse.Should().HaveStatusCode(HttpStatusCode.OK, "token generation must succeed for test setup");

        var generateBody = await generateResponse.Content.ReadFromJsonAsync<ApiResponse<GenerateTokenResponse>>();
        var jti = AuthenticatedClientHelper.ExtractJti(generateBody!.Data!.Token);

        // Act
        var response = await client.PostAsync($"/api/v2.0/auth/tokens/{jti}/revoke", null);

        // Assert — HTTP response
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

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
        await firstResponse.Should().HaveStatusCode(HttpStatusCode.OK, "first token generation must succeed for test setup");

        var secondResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", secondRequest);
        await secondResponse.Should().HaveStatusCode(HttpStatusCode.OK, "second token generation must succeed for test setup");

        // Act
        var response = await client.GetAsync("/api/v2.0/auth/tokens?Page=1&PageSize=50");

        // Assert — HTTP response
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>>();
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
        var client = await AuthenticateAdminAsync();
        var firstRequest = new
        {
            UsernameSuffix = "duplicate",
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", firstRequest);
        await firstResponse.Should().HaveStatusCode(HttpStatusCode.OK, "first token generation must succeed for test setup");

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

    private async Task<HttpClient> AuthenticateAdminAsync()
        => await _factory.AuthenticateAsync("Admin", "adminsecret");
}
