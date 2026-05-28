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

    [Theory]
    [InlineData("/alive")]
    [InlineData("/ready")]
    public async Task HealthProbeEndpoint_AnonymousRequest_ReturnsHealthyWithBody(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthEndpoint_AnonymousRequest_ReturnsHealthyJsonWithDaprCheck()
    {
        // Arrange

        // Act
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
}
