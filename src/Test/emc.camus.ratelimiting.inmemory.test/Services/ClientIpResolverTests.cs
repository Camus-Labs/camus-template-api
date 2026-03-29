using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using emc.camus.ratelimiting.inmemory.Services;
using System.Net;

namespace emc.camus.ratelimiting.inmemory.test.Services;

public class ClientIpResolverTests
{
    private const string ValidIpV4 = "192.168.1.1";

    private static ClientIpResolver CreateResolver() =>
        new(NullLogger<ClientIpResolver>.Instance);

    private static DefaultHttpContext CreateHttpContext(
        string? forwardedFor = null,
        string? realIp = null,
        IPAddress? remoteIp = null)
    {
        var context = new DefaultHttpContext();

        if (forwardedFor != null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        if (realIp != null)
        {
            context.Request.Headers["X-Real-IP"] = realIp;
        }

        if (remoteIp != null)
        {
            context.Connection.RemoteIpAddress = remoteIp;
        }

        return context;
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new ClientIpResolver(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    // --- GetClientIpAddress: Null Context ---

    [Fact]
    public void GetClientIpAddress_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var resolver = CreateResolver();

        // Act
        var act = () => resolver.GetClientIpAddress(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("context");
    }

    // --- GetClientIpAddress: X-Forwarded-For ---

    [Theory]
    [InlineData("192.168.1.1", "192.168.1.1")]
    [InlineData("192.168.1.1, 10.0.0.1", "192.168.1.1")]
    [InlineData("::1", "::1")]
    public void GetClientIpAddress_ValidForwardedFor_ReturnsExpectedIp(string forwardedFor, string expectedIp)
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(forwardedFor: forwardedFor);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp);
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("")]
    [InlineData("   ")]
    public void GetClientIpAddress_InvalidOrEmptyForwardedFor_FallsBackToRealIp(string forwardedFor)
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(forwardedFor: forwardedFor, realIp: ValidIpV4);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    // --- GetClientIpAddress: X-Real-IP ---

    [Fact]
    public void GetClientIpAddress_ValidRealIp_ReturnsRealIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(realIp: ValidIpV4);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    [Theory]
    [InlineData("not-valid")]
    [InlineData("")]
    public void GetClientIpAddress_InvalidOrEmptyRealIp_FallsBackToRemoteIp(string realIp)
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(realIp: realIp, remoteIp: IPAddress.Parse(ValidIpV4));

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    // --- GetClientIpAddress: Remote IP ---

    public static readonly IEnumerable<object[]> RemoteIpTestCases = new[]
    {
        new object[] { IPAddress.Parse("192.168.1.1"), "192.168.1.1" },
        new object[] { IPAddress.IPv6Loopback, "::1" }
    };

    [Theory]
    [MemberData(nameof(RemoteIpTestCases))]
    public void GetClientIpAddress_RemoteIpOnly_ReturnsExpectedIp(IPAddress remoteIp, string expectedIp)
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(remoteIp: remoteIp);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(expectedIp);
    }

    // --- GetClientIpAddress: No IP Available ---

    [Fact]
    public void GetClientIpAddress_NoIpAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext();

        // Act
        var act = () => resolver.GetClientIpAddress(context);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unable to determine client IP*");
    }

    // --- GetClientIpAddress: Priority Order ---

    [Fact]
    public void GetClientIpAddress_AllSourcesPresent_PrefersForwardedFor()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(
            forwardedFor: ValidIpV4,
            realIp: "10.0.0.1",
            remoteIp: IPAddress.Loopback);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    [Fact]
    public void GetClientIpAddress_RealIpAndRemoteIpPresent_PrefersRealIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(
            realIp: ValidIpV4,
            remoteIp: IPAddress.Parse("10.0.0.1"));

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }
}
