using System.Net;
using System.Net.Http.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V1;
using FluentAssertions;

namespace emc.camus.api.integration.test.ApiInfo;

[Trait("Category", "Integration")]
[Collection(PostgreSqlTestGroup.Name)]
public class ApiInfoPostgreSqlEndpointTests : IAsyncLifetime
{
    private readonly ApiPostgreSqlFactory _factory;
    private readonly HttpClient _client;

    public ApiInfoPostgreSqlEndpointTests(ApiPostgreSqlFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync() => await _factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task GetInfo_PostgreSqlPersistence_ReturnsOkWithApiInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/apiinfo/info", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ApiInfoResponse>>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Version.Should().Be("1.0");
        body.Data.Status.Should().NotBeNullOrWhiteSpace();
        body.Data.Features.Should().NotBeEmpty();
    }
}
