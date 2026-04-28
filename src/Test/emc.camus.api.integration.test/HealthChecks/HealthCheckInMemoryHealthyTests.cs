using System.Net;
using System.Text.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using FluentAssertions;

namespace emc.camus.api.integration.test.HealthChecks;

[Trait("Category", "Integration")]
[Collection(InMemoryTestGroup.Name)]
public class HealthCheckInMemoryHealthyTests
{
    private readonly HttpClient _client;

    public HealthCheckInMemoryHealthyTests(ApiInMemoryFactory factory, ITestOutputHelper outputHelper)
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
    public async Task HealthEndpoint_AnonymousRequest_ReturnsHealthyJsonWithDaprCheck()
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
        checks.Should().ContainSingle();
        checks[0].GetProperty("name").GetString().Should().Be("dapr-secrets");
        checks[0].GetProperty("status").GetString().Should().Be("Healthy");
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
