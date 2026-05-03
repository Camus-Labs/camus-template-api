using System.Net;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.application.ApiInfo;
using emc.camus.application.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace emc.camus.api.integration.test.Common;

[Trait("Category", "Integration")]
[Collection(TimeoutTestGroup.Name)]
public class RequestTimeoutInMemoryTests
{
    private readonly ApiTimeoutFactory _factory;

    public RequestTimeoutInMemoryTests(ApiTimeoutFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    [Fact]
    public async Task GetInfo_RequestExceedsTightTimeout_Returns504GatewayTimeout()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/apiinfo/info", TestContext.Current.CancellationToken);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.GatewayTimeout);
        await response.Should().HaveErrorCode(ErrorCodes.RequestTimeout);
    }

    [Fact]
    public async Task GetInfo_ClientDisconnects_CancellationPropagesToServiceLayer()
    {
        // Arrange
        var client = _factory.CreateClient();
        var slowService = _factory.Services.GetRequiredService<SlowApiInfoService>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act — client cancels before server responds
        var act = () => client.GetAsync("/api/v1/apiinfo/info", cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        // Assert — server-side cancellation propagated to the service layer
        var propagated = await Task.WhenAny(
            slowService.CancellationReceived,
            Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        propagated.Should().Be(slowService.CancellationReceived,
            "cancellation token should have propagated to SlowApiInfoService");
    }
}
