using System.Net;
using System.Net.Http.Json;
using Dapper;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;
using emc.camus.application.Auth;
using FluentAssertions;

namespace emc.camus.api.integration.test.InMemoryCache;

[Trait("Category", "Integration")]
[Collection(PostgreSqlTestGroup.Name)]
public class TokenRevocationCachePostgreSqlTests : IAsyncLifetime
{
    private readonly ApiPostgreSqlFactory _factory;

    public TokenRevocationCachePostgreSqlTests(ApiPostgreSqlFactory factory, ITestOutputHelper outputHelper)
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
    public async Task BackgroundSync_RevokedTokenInDatabase_CacheRejectsTokenAfterSyncCycle()
    {
        // Arrange — generate a token, then revoke it directly in the DB (bypassing the API/cache)
        var adminClient = await _factory.AuthenticateAsync("Admin", "adminsecret");
        var (generatedToken, jti) = await GenerateTokenAsync(adminClient, "bg-sync");

        // Verify the token works before DB-only revocation
        var tokenClient = _factory.CreateClient();
        tokenClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", generatedToken);

        var preRevokeResponse = await tokenClient.GetAsync("/api/v2.0/apiinfo/info-jwt", TestContext.Current.CancellationToken);
        await preRevokeResponse.Should().HaveStatusCode(HttpStatusCode.OK, "token must be accepted before revocation");

        // Revoke directly in the database — the cache does NOT know about this yet
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.ExecuteAsync(
            "UPDATE camus.generated_tokens SET is_revoked = true, revoked_at = @Now WHERE jti = @Jti",
            new { Jti = jti, Now = DateTime.UtcNow });

        // Act — wait for the background sync service to pick up the revocation (10s interval)
        var cache = _factory.Services.GetRequiredService<ITokenRevocationCache>();
        var synced = await PollUntilAsync(() => cache.IsRevoked(jti), timeout: TimeSpan.FromSeconds(15));
        synced.Should().BeTrue("background sync service should load revoked JTIs from the database within the sync interval");

        // Assert — the token is now rejected by the JWT validation pipeline
        var response = await tokenClient.GetAsync("/api/v2.0/apiinfo/info-jwt", TestContext.Current.CancellationToken);
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        await response.Should().HaveErrorCode("jwt_token_revoked");
    }

    private static async Task<bool> PollUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!cts.Token.IsCancellationRequested)
        {
            if (condition())
                return true;

            await Task.Delay(500, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        return condition();
    }

    private static async Task<(string Token, Guid Jti)> GenerateTokenAsync(HttpClient client, string usernameSuffix)
    {
        var request = new
        {
            UsernameSuffix = usernameSuffix,
            ExpiresOn = DateTime.UtcNow.AddHours(2),
            Permissions = new[] { "api.read" },
        };

        var response = await client.PostAsJsonAsync("/api/v2.0/auth/generate-token", request, TestContext.Current.CancellationToken);
        await response.Should().HaveStatusCode(HttpStatusCode.Created, "token generation must succeed for test setup");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GenerateTokenResponse>>(TestContext.Current.CancellationToken);
        var token = body!.Data!.Token;
        var jti = AuthenticatedClientHelper.ExtractJti(token);

        return (token, jti);
    }
}
