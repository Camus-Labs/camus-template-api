using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using emc.camus.api.Controllers;
using emc.camus.api.Models.Dtos.V2;
using emc.camus.api.Models.Requests.V2;
using emc.camus.api.Models.Responses;
using emc.camus.api.Models.Responses.V2;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using emc.camus.application.Observability;

namespace emc.camus.api.test.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IActivitySourceWrapper> _mockActivitySource;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockActivitySource = new Mock<IActivitySourceWrapper>();
        _mockActivitySource
            .Setup(x => x.StartActivityAndRunAsync<IActionResult>(
                It.IsAny<string>(),
                It.IsAny<OperationType>(),
                It.IsAny<Func<Activity?, Task<IActionResult>>>()))
            .Returns<string, OperationType, Func<Activity?, Task<IActionResult>>>(
                (_, _, func) => func(null));

        _mockAuthService = new Mock<IAuthService>();

        _controller = new AuthController(_mockActivitySource.Object, _mockAuthService.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // --- Constructor ---

    public static IEnumerable<object?[]> Constructor_NullDependencyScenarios()
    {
        var activitySource = new Mock<IActivitySourceWrapper>().Object;
        var authService = new Mock<IAuthService>().Object;

        yield return new object?[] { null, authService };
        yield return new object?[] { activitySource, null };
    }

    [Theory]
    [MemberData(nameof(Constructor_NullDependencyScenarios))]
    public void Constructor_NullDependency_ThrowsArgumentNullException(
        IActivitySourceWrapper? activitySource, IAuthService? authService)
    {
        // Arrange
        // Act
        var act = () => new AuthController(activitySource!, authService!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- AuthenticateUser ---

    [Fact]
    public async Task AuthenticateUser_ValidRequest_ReturnsOkWithAuthResponseAndSetsActivityTags()
    {
        // Arrange
        var request = new AuthenticateUserRequest
        {
            Username = "testuser",
            Password = "securepass"
        };
        var authResult = new AuthenticateUserResult("jwt-token-value", new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc));

        _mockAuthService
            .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticateUserCommand>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.AuthenticateUser(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AuthenticateUserResponse>>().Subject;
        apiResponse.Data!.Token.Should().Be("jwt-token-value");
        apiResponse.Data.ExpiresOn.Should().Be(authResult.ExpiresOn);
        apiResponse.Message.Should().Contain("authenticated successfully");

        _mockActivitySource.Verify(
            a => a.SetRequestTags(It.IsAny<Activity?>(), It.IsAny<IDictionary<string, object?>>()),
            Times.Once);
        _mockActivitySource.Verify(
            a => a.SetResponseTags(It.IsAny<Activity?>(), It.IsAny<IDictionary<string, object?>>()),
            Times.Once);
    }

    // --- GenerateToken ---

    [Fact]
    public async Task GenerateToken_ValidRequest_ReturnsOkWithGenerateTokenResponse()
    {
        // Arrange
        var request = new GenerateTokenRequest
        {
            UsernameSuffix = "ci-deploy",
            ExpiresOn = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            Permissions = new List<string> { "api.read" }
        };
        var generateResult = new GenerateTokenResult(
            "generated-token",
            request.ExpiresOn,
            "adminuser-ci-deploy");

        _mockAuthService
            .Setup(s => s.GenerateTokenAsync(It.IsAny<GenerateTokenCommand>()))
            .ReturnsAsync(generateResult);

        // Act
        var result = await _controller.GenerateToken(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GenerateTokenResponse>>().Subject;
        apiResponse.Data!.Token.Should().Be("generated-token");
        apiResponse.Data.ExpiresOn.Should().Be(generateResult.ExpiresOn);
        apiResponse.Data.TokenUsername.Should().Be("adminuser-ci-deploy");
        apiResponse.Message.Should().Contain("Token generated successfully");
    }

    // --- GetGeneratedTokens ---

    [Fact]
    public async Task GetGeneratedTokens_ValidRequest_ReturnsOkWithPagedResponse()
    {
        // Arrange
        var query = new GetGeneratedTokensQuery
        {
            Page = 1,
            PageSize = 10,
            ExcludeRevoked = true,
            ExcludeExpired = false
        };

        var jti = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var view = new GeneratedTokenSummaryView(
            jti, "admin-token1",
            new List<string> { "api.read" },
            new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            false, null, true);

        var pagedResult = new PagedResult<GeneratedTokenSummaryView>(
            new List<GeneratedTokenSummaryView> { view }, 1, 1, 10);

        _mockAuthService
            .Setup(s => s.GetGeneratedTokensAsync(
                It.IsAny<PaginationParams>(),
                It.IsAny<GeneratedTokenFilter>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetGeneratedTokens(query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>>().Subject;
        apiResponse.Data!.Items.Should().ContainSingle();
        apiResponse.Data.Items[0].Jti.Should().Be(jti);
        apiResponse.Data.Items[0].TokenUsername.Should().Be("admin-token1");
        apiResponse.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetGeneratedTokens_EmptyResult_ReturnsOkWithEmptyPage()
    {
        // Arrange
        var query = new GetGeneratedTokensQuery();
        var pagedResult = new PagedResult<GeneratedTokenSummaryView>(
            new List<GeneratedTokenSummaryView>(), 0, 1, 25);

        _mockAuthService
            .Setup(s => s.GetGeneratedTokensAsync(
                It.IsAny<PaginationParams>(),
                It.IsAny<GeneratedTokenFilter>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetGeneratedTokens(query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<GeneratedTokenSummaryDto>>>().Subject;
        apiResponse.Data!.Items.Should().BeEmpty();
        apiResponse.Data.TotalCount.Should().Be(0);
    }

    // --- RevokeToken ---

    [Fact]
    public async Task RevokeToken_ValidJti_ReturnsOkWithRevokedTokenSummary()
    {
        // Arrange
        var revokedAt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var jti = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var view = new GeneratedTokenSummaryView(
            jti, "admin-token1",
            new List<string> { "api.read" },
            new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            true, revokedAt, false);

        _mockAuthService
            .Setup(s => s.RevokeTokenAsync(It.IsAny<RevokeTokenCommand>()))
            .ReturnsAsync(view);

        // Act
        var result = await _controller.RevokeToken(jti);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GeneratedTokenSummaryDto>>().Subject;
        apiResponse.Data!.Jti.Should().Be(jti);
        apiResponse.Data.IsRevoked.Should().BeTrue();
        apiResponse.Data.RevokedAt.Should().Be(revokedAt);
        apiResponse.Message.Should().Contain("Token revoked successfully");
    }
}
