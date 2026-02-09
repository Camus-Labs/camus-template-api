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
    public void GetClientIpAddress_WithSingleXForwardedFor_ShouldReturnThatIp()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.42";

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("203.0.113.42");
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
    public void GetClientIpAddress_WithNoHeaders_AndNoRemoteIp_ShouldReturnUnknown()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("unknown");
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

    [Fact]
    public void GetClientIpAddress_WithMultipleIpsInXForwardedFor_ShouldReturnFirst()
    {
        // Arrange
        var resolver = new ClientIpResolver(_mockLogger.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.1, 203.0.113.2, 203.0.113.3";

        // Act
        var result = resolver.GetClientIpAddress(context);

        // Assert
        result.Should().Be("203.0.113.1");
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("203.0.113.42")]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
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
}
