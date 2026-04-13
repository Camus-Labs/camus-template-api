using System.Net;
using System.Net.Http.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V1;
using FluentAssertions;
using Xunit.Abstractions;

namespace emc.camus.api.integration.test.ApiInfo;

[Trait("Category", "Integration")]
[Collection(InMemoryTestGroup.Name)]
public class ApiInfoInMemoryEndpointTests
{
    private readonly ApiInMemoryFactory _factory;
    private readonly HttpClient _client;

    public ApiInfoInMemoryEndpointTests(ApiInMemoryFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInfo_PublicEndpoint_ReturnsOkWithApiInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/v1.0/apiinfo/info");

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ApiInfoResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Version.Should().Be("1.0");
        body.Data.Status.Should().NotBeNullOrWhiteSpace();
        body.Data.Features.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetInfoApiKey_ValidApiKey_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateApiKeyClient();

        // Act
        var response = await client.GetAsync("/api/v2.0/apiinfo/info-apikey");

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ApiInfoResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Version.Should().Be("2.0");
    }

    [Fact]
    public async Task GetInfoApiKey_NoApiKey_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v2.0/apiinfo/info-apikey");

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        await response.Should().HaveErrorCode("apikey_authentication_required");
    }

    [Fact]
    public async Task GetInfoJwt_ValidJwtToken_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateJwtClient();

        // Act
        var response = await client.GetAsync("/api/v2.0/apiinfo/info-jwt");

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ApiInfoResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Version.Should().Be("2.0");
    }

    [Fact]
    public async Task GetInfoJwt_NoToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v2.0/apiinfo/info-jwt");

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        await response.Should().HaveErrorCode("jwt_authentication_required");
    }
}
