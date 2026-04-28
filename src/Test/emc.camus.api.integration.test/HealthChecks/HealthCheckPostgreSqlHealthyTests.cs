using System.Net;
using System.Text.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using FluentAssertions;

namespace emc.camus.api.integration.test.HealthChecks;

[Trait("Category", "Integration")]
[Collection(PostgreSqlTestGroup.Name)]
public class HealthCheckPostgreSqlHealthyTests
{
    private readonly HttpClient _client;

    public HealthCheckPostgreSqlHealthyTests(ApiPostgreSqlFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AliveEndpoint_AnonymousRequest_ReturnsHealthyWithBody()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/alive", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthEndpoint_AnonymousRequest_ReturnsHealthyJsonWithBothChecks()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        var root = json.RootElement;

        root.GetProperty("status").GetString().Should().Be("Healthy");

        var checks = root.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().HaveCount(2);

        var checkNames = checks.Select(c => c.GetProperty("name").GetString()).ToList();
        checkNames.Should().Contain("dapr-secrets");
        checkNames.Should().Contain("postgresql");

        checks.Should().OnlyContain(c => c.GetProperty("status").GetString() == "Healthy");
    }

    [Fact]
    public async Task ReadyEndpoint_AnonymousRequest_ReturnsHealthyWithBody()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/ready", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Healthy");
    }
}
