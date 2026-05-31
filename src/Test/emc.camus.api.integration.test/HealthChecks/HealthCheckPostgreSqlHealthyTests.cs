using System.Net;
using System.Net.Mime;
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
    public async Task HealthEndpoint_AnonymousRequest_ReturnsHealthyJsonWithBothChecks()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Json);

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

}
