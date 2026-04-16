using System.Diagnostics;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using emc.camus.api.Infrastructure;
using emc.camus.application.Auth;

namespace emc.camus.api.test.Infrastructure;

public class HttpUserContextTests
{
    private static readonly Guid FixedUserId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static HttpUserContext CreateContext(ClaimsPrincipal? user = null, bool hasHttpContext = true)
    {
        var mockAccessor = new Mock<IHttpContextAccessor>();

        if (hasHttpContext)
        {
            var httpContext = new DefaultHttpContext();
            if (user != null)
            {
                httpContext.User = user;
            }
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        }
        else
        {
            HttpContext? noContext = null;
            mockAccessor.Setup(x => x.HttpContext).Returns(noContext);
        }

        return new HttpUserContext(mockAccessor.Object);
    }

    private static HttpUserContext CreateContextWithNullUser()
    {
        var mockAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(default(ClaimsPrincipal)!);
        mockAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
        return new HttpUserContext(mockAccessor.Object);
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(
        string username = "testuser",
        Guid? userId = null,
        List<string>? permissions = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.NameIdentifier, (userId ?? FixedUserId).ToString())
        };

        if (permissions != null)
        {
            claims.AddRange(permissions.Select(p => new Claim(Permissions.ClaimType, p)));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullHttpContextAccessor_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new HttpUserContext(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // --- GetCurrentUserId ---

    [Fact]
    public void GetCurrentUserId_AuthenticatedUser_ReturnsUserId()
    {
        // Arrange
        var user = CreateAuthenticatedUser(userId: FixedUserId);
        var context = CreateContext(user);

        // Act
        var result = context.GetCurrentUserId();

        // Assert
        result.Should().Be(FixedUserId);
    }

    public static IEnumerable<object[]> GetCurrentUserId_NullScenarios()
    {
        yield return new object[] { CreateContext() };
        yield return new object[] { CreateContext(hasHttpContext: false) };
        yield return new object[] { CreateContextWithNullUser() };

        var nullIdentityUser = new ClaimsPrincipal();
        yield return new object[] { CreateContext(nullIdentityUser) };

        var nonGuidUser = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, "TestAuth"));
        yield return new object[] { CreateContext(nonGuidUser) };

        var missingIdUser = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        yield return new object[] { CreateContext(missingIdUser) };
    }

    [Theory]
    [MemberData(nameof(GetCurrentUserId_NullScenarios))]
    public void GetCurrentUserId_UserIdNotAvailable_ReturnsNull(HttpUserContext context)
    {
        // Arrange
        // Act
        var result = context.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    // --- GetCurrentUsername ---

    [Fact]
    public void GetCurrentUsername_AuthenticatedUser_ReturnsUsername()
    {
        // Arrange
        var user = CreateAuthenticatedUser(username: "admin");
        var context = CreateContext(user);

        // Act
        var result = context.GetCurrentUsername();

        // Assert
        result.Should().Be("admin");
    }

    public static IEnumerable<object[]> GetCurrentUsername_NullScenarios()
    {
        yield return new object[] { CreateContext() };
        yield return new object[] { CreateContext(hasHttpContext: false) };
        yield return new object[] { CreateContextWithNullUser() };

        var nullIdentityUser = new ClaimsPrincipal();
        yield return new object[] { CreateContext(nullIdentityUser) };
    }

    [Theory]
    [MemberData(nameof(GetCurrentUsername_NullScenarios))]
    public void GetCurrentUsername_UsernameNotAvailable_ReturnsNull(HttpUserContext context)
    {
        // Arrange
        // Act
        var result = context.GetCurrentUsername();

        // Assert
        result.Should().BeNull();
    }

    // --- GetCurrentPermissions ---

    [Fact]
    public void GetCurrentPermissions_AuthenticatedUserWithPermissions_ReturnsPermissions()
    {
        // Arrange
        var permissions = new List<string> { "api.read", "api.write" };
        var user = CreateAuthenticatedUser(permissions: permissions);
        var context = CreateContext(user);

        // Act
        var result = context.GetCurrentPermissions();

        // Assert
        result.Should().BeEquivalentTo(permissions);
    }

    public static IEnumerable<object[]> GetCurrentPermissions_EmptyScenarios()
    {
        yield return new object[] { CreateContext(CreateAuthenticatedUser()) };
        yield return new object[] { CreateContext() };
        yield return new object[] { CreateContext(hasHttpContext: false) };
        yield return new object[] { CreateContextWithNullUser() };

        var nullIdentityUser = new ClaimsPrincipal();
        yield return new object[] { CreateContext(nullIdentityUser) };
    }

    [Theory]
    [MemberData(nameof(GetCurrentPermissions_EmptyScenarios))]
    public void GetCurrentPermissions_PermissionsNotAvailable_ReturnsEmptyList(
        HttpUserContext context)
    {
        // Arrange
        // Act
        var result = context.GetCurrentPermissions();

        // Assert
        result.Should().BeEmpty();
    }

    // --- GetCurrentTraceId ---

    [Fact]
    public void GetCurrentTraceId_NoActiveActivity_ReturnsNull()
    {
        // Arrange
        var context = CreateContext();
        Activity.Current = null;

        // Act
        var result = context.GetCurrentTraceId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentTraceId_ActiveActivity_ReturnsTraceId()
    {
        // Arrange
        var context = CreateContext();
        using var activitySource = new ActivitySource("test-source");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = activitySource.StartActivity("test-activity");

        // Act
        var result = context.GetCurrentTraceId();

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }
}
