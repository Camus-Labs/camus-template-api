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
    private const string ValidIpV6 = "::1";
    private const string AnotherValidIpV4 = "10.0.0.1";

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

    [Fact]
    public void GetClientIpAddress_ValidForwardedFor_ReturnsFirstIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(forwardedFor: ValidIpV4);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    [Fact]
    public void GetClientIpAddress_MultipleForwardedForIps_ReturnsFirstIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(forwardedFor: $"{ValidIpV4}, {AnotherValidIpV4}");

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    [Fact]
    public void GetClientIpAddress_ForwardedForWithIpV6_ReturnsIpV6()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(forwardedFor: ValidIpV6);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV6);
    }

    [Fact]
    public void GetClientIpAddress_InvalidForwardedFor_FallsBackToRealIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(forwardedFor: "not-an-ip", realIp: ValidIpV4);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    [Fact]
    public void GetClientIpAddress_EmptyForwardedFor_FallsBackToRealIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(forwardedFor: "", realIp: ValidIpV4);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    [Fact]
    public void GetClientIpAddress_WhitespaceOnlyForwardedFor_FallsBackToRealIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(forwardedFor: "   ", realIp: ValidIpV4);

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

    [Fact]
    public void GetClientIpAddress_InvalidRealIp_FallsBackToRemoteIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(realIp: "not-valid", remoteIp: IPAddress.Parse(ValidIpV4));

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    [Fact]
    public void GetClientIpAddress_EmptyRealIp_FallsBackToRemoteIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(realIp: "", remoteIp: IPAddress.Parse(ValidIpV4));

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    // --- GetClientIpAddress: Remote IP ---

    [Fact]
    public void GetClientIpAddress_RemoteIpOnly_ReturnsRemoteIp()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(remoteIp: IPAddress.Parse(ValidIpV4));

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }

    [Fact]
    public void GetClientIpAddress_RemoteIpV6_ReturnsIpV6()
    {
        // Arrange
        var resolver = CreateResolver();
        var context = CreateHttpContext(remoteIp: IPAddress.IPv6Loopback);

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV6);
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
            realIp: AnotherValidIpV4,
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
            remoteIp: IPAddress.Parse(AnotherValidIpV4));

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ValidIpV4);
    }
}
