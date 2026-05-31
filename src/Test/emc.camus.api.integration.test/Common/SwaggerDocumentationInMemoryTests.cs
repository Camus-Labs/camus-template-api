using System.Net;
using System.Text.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using FluentAssertions;

namespace emc.camus.api.integration.test.Common;

[Trait("Category", "Integration")]
[Collection(SwaggerTestGroup.Name)]
public class SwaggerDocumentationInMemoryTests
{
    private readonly ApiSwaggerFactory _factory;

    private const string SwaggerJsonV1Endpoint = "/swagger/v1/swagger.json";

    public SwaggerDocumentationInMemoryTests(ApiSwaggerFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    [Fact]
    public async Task GetSwaggerJson_V1Enabled_ReturnsValidOpenApiDocument()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(SwaggerJsonV1Endpoint, TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        root.GetProperty("openapi").GetString().Should().StartWith("3.");
        root.GetProperty("info").GetProperty("title").GetString().Should().Be("Camus API v1.0 Basic Demo");
        root.GetProperty("info").GetProperty("version").GetString().Should().Be("v1");
    }
}
