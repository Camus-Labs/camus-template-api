using emc.camus.ratelimiting.memory.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace emc.camus.ratelimiting.memory.test.Services;

/// <summary>
/// Unit tests for ClientIpResolver to verify IP address resolution logic for rate limiting.
/// </summary>
public class ClientIpResolverTests
{
    private readonly Mock<ILogger<ClientIpResolver>> _mockLogger;

    public ClientIpResolverTests()
    {
        _mockLogger = new Mock<ILogger<ClientIpResolver>>();
    }

    [Fact]
    public void GetClientIpAddress_WithXForwardedFor_ShouldReturnFirstIp()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1, 10.0.0.1, 172.16.0.1";

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("192.168.1.1");
    }

    [Fact]
    public void GetClientIpAddress_WithXRealIp_ShouldReturnRealIp()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Real-IP"] = "198.51.100.23";

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("198.51.100.23");
    }

    [Fact]
    public void GetClientIpAddress_WithBothHeaders_ShouldPreferXForwardedFor()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1";
        context.Request.Headers["X-Real-IP"] = "10.0.0.1";

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("192.168.1.1");
    }

    [Fact]
    public void GetClientIpAddress_WithDirectConnection_ShouldReturnRemoteIpAddress()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.195");

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("203.0.113.195");
    }

    [Fact]
    public void GetClientIpAddress_WithNoHeaders_AndNoRemoteIp_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();

        // Act
        var act = () => resolver.GetClientIpAddress(context);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unable to determine client IP address for rate limiting.");
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unable to determine client IP address")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetClientIpAddress_WithInvalidXForwardedFor_ShouldTryXRealIp()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "invalid-ip-format";
        context.Request.Headers["X-Real-IP"] = "198.51.100.23";

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("198.51.100.23");
    }

    [Fact]
    public void GetClientIpAddress_WithInvalidXForwardedFor_ShouldLogWarning()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers["X-Forwarded-For"] = "not-a-valid-ip";
        // Without fall back it will throw exception, so we need to set remote ip to avoid that and reach the logging code for invalid x-forwarded-for header.
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.195");

        // Act
        resolver.GetClientIpAddress(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid IP format")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetClientIpAddress_WithEmptyXForwardedFor_ShouldTryXRealIp()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "";
        context.Request.Headers["X-Real-IP"] = "198.51.100.23";

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("198.51.100.23");
    }

    [Fact]
    public void GetClientIpAddress_WithWhitespaceXForwardedFor_ShouldTryXRealIp()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "   ";
        context.Request.Headers["X-Real-IP"] = "198.51.100.23";

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("198.51.100.23");
    }

    [Theory]
    [InlineData("192.168.1.1")]                                     // Private IPv4 Class C
    [InlineData("10.0.0.1")]                                        // Private IPv4 Class A
    [InlineData("172.16.0.1")]                                      // Private IPv4 Class B
    [InlineData("203.0.113.42")]                                    // Public IPv4
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334")]        // IPv6 full format
    public void GetClientIpAddress_WithVariousValidIps_ShouldReturnCorrectIp(string ipAddress)
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = ipAddress;

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be(ipAddress);
    }

    [Fact]
    public void GetClientIpAddress_WithXForwardedForContainingSpaces_ShouldTrimAndReturnFirst()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = " 192.168.1.1 , 10.0.0.1 ";

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("192.168.1.1");
    }

    [Fact]
    public void GetClientIpAddress_WithIPv6_ShouldHandleCorrectly()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("::1");

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("::1");
    }

    [Fact]
    public void GetClientIpAddress_WithInvalidXRealIp_ShouldFallbackToRemoteIp()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Real-IP"] = "invalid-ip-format";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.195");

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("203.0.113.195");
    }

    [Fact]
    public void GetClientIpAddress_WithEmptyXRealIp_ShouldFallbackToRemoteIp()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Real-IP"] = "";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.195");

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("203.0.113.195");
    }

    [Fact]
    public void GetClientIpAddress_WithWhitespaceXRealIp_ShouldFallbackToRemoteIp()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Real-IP"] = "   ";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.195");

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("203.0.113.195");
    }
}
