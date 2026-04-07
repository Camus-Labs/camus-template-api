using System.Net;
using System.Net.Http.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V1;
using FluentAssertions;
using Xunit.Abstractions;

namespace emc.camus.api.integration.test.ApiInfo;

[Trait("Category", "Integration")]
public class ApiInfoPSEndpointTests : IClassFixture<CamusApiPSFactory>
{
    private readonly HttpClient _client;

    public ApiInfoPSEndpointTests(CamusApiPSFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInfo_PostgreSqlPersistence_ReturnsOkWithApiInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/apiinfo/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ApiInfoResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Version.Should().Be("1.0");
        body.Data.Status.Should().NotBeNullOrWhiteSpace();
        body.Data.Features.Should().NotBeEmpty();
    }
}
