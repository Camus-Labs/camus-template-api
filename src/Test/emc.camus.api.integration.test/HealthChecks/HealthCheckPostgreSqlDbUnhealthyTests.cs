using System.Net;
using System.Text.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using FluentAssertions;

namespace emc.camus.api.integration.test.HealthChecks;

[Trait("Category", "Integration")]
[Collection(PostgreSqlTestGroup.Name)]
public class HealthCheckPostgreSqlDbUnhealthyTests : IDisposable
{
    private readonly HttpClient _client;

    public HealthCheckPostgreSqlDbUnhealthyTests(ApiPostgreSqlFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _client = factory.CreateClient();
        UnitOfWorkConnectivityDecorator.SimulateConnectivityFailure = true;
    }

    public void Dispose()
    {
        UnitOfWorkConnectivityDecorator.SimulateConnectivityFailure = false;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AliveEndpoint_DatabaseUnavailable_StillReturnsHealthy()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/alive", TestContext.Current.CancellationToken);

        // Assert — liveness probe skips all checks, always healthy
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthEndpoint_DatabaseUnavailable_ReturnsUnhealthyJsonWithSecretStoreStillHealthy()
    {
        // Arrange & Act
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

        var dbCheck = checks.Should().ContainSingle(c => c.GetProperty("name").GetString() == "postgresql").Which;
        dbCheck.GetProperty("status").GetString().Should().Be("Unhealthy");
        dbCheck.GetProperty("description").GetString().Should().Be("PostgreSQL database is unreachable");

        var daprCheck = checks.Should().ContainSingle(c => c.GetProperty("name").GetString() == "dapr-secrets").Which;
        daprCheck.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task ReadyEndpoint_DatabaseUnavailable_ReturnsUnhealthyWithBody()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/ready", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Unhealthy");
    }
}
