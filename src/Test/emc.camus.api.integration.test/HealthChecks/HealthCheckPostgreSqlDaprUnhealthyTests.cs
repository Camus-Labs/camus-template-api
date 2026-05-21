using System.Net;
using System.Text.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.application.Secrets;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace emc.camus.api.integration.test.HealthChecks;

[Trait("Category", "Integration")]
[Collection(PostgreSqlTestGroup.Name)]
public class HealthCheckPostgreSqlDaprUnhealthyTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly StubSecretProvider _stubSecrets;

    public HealthCheckPostgreSqlDaprUnhealthyTests(ApiPostgreSqlFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _client = factory.CreateClient();
        // Justification: secret-store connectivity failure cannot be triggered via HTTP — requires toggling the stub's SimulateConnectivityFailure flag.
        _stubSecrets = (StubSecretProvider)factory.Services.GetRequiredService<ISecretProvider>();
        _stubSecrets.SimulateConnectivityFailure = true;
    }

    public void Dispose()
    {
        _stubSecrets.SimulateConnectivityFailure = false;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AliveEndpoint_SecretStoreUnreachable_StillReturnsHealthy()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync("/alive", TestContext.Current.CancellationToken);

        // Assert — liveness probe skips all checks, always healthy
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthEndpoint_SecretStoreUnreachable_ReturnsUnhealthyJsonWithDbStillHealthy()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        var root = json.RootElement;

        root.GetProperty("status").GetString().Should().Be("Unhealthy");

        var checks = root.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().HaveCount(2);

        var daprCheck = checks.Should().ContainSingle(c => c.GetProperty("name").GetString() == "dapr-secrets").Which;
        daprCheck.GetProperty("status").GetString().Should().Be("Unhealthy");
        daprCheck.GetProperty("description").GetString().Should().Be("Dapr secret store is unreachable");

        var dbCheck = checks.Should().ContainSingle(c => c.GetProperty("name").GetString() == "postgresql").Which;
        dbCheck.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task ReadyEndpoint_SecretStoreUnreachable_ReturnsUnhealthyWithBody()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync("/ready", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Unhealthy");
    }
}
