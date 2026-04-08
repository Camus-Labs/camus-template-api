using System.Net;
using System.Net.Http.Json;
using emc.camus.api.integration.test.Fixtures;
using emc.camus.api.integration.test.Helpers;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;
using FluentAssertions;
using Xunit.Abstractions;

namespace emc.camus.api.integration.test.Auth;

[Trait("Category", "Integration")]
[Collection(InMemoryTestGroup.Name)]
public class AuthIMEndpointTests
{
    private readonly ApiIMFactory _factory;

    public AuthIMEndpointTests(ApiIMFactory factory, ITestOutputHelper outputHelper)
    {
        factory.OutputHelper = outputHelper;
        _factory = factory;
    }

    [Fact]
    public async Task Authenticate_ValidAdminCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var client = _factory.CreateApiKeyClient();
        var request = new { Username = "admin", Password = "admin-password" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v2.0/auth/authenticate", request);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthenticateUserResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Token.Should().NotBeNullOrWhiteSpace();
        body.Data.ExpiresOn.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Authenticate_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateApiKeyClient();
        var request = new { Username = "admin", Password = "wrong-password" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v2.0/auth/authenticate", request);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticate_NoApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { Username = "admin", Password = "admin-password" };

        // Act
        var response = await client.PostAsJsonAsync("/api/v2.0/auth/authenticate", request);

        // Assert
        await response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }
}
