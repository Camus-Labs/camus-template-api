using emc.camus.application.Exceptions;
using FluentAssertions;

namespace emc.camus.application.test.Exceptions;

/// <summary>
/// Unit tests for RateLimitExceededException to verify exception behavior and properties.
/// </summary>
public class RateLimitExceededExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var policyName = "default";
        var permitLimit = 100;
        var windowSeconds = 60;
        var retryAfterSeconds = 30;
        var resetTimestamp = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds();

        // Act
        var exception = new RateLimitExceededException(
            policyName,
            permitLimit,
            windowSeconds,
            retryAfterSeconds,
            resetTimestamp);

        // Assert
        exception.PolicyName.Should().Be(policyName);
        exception.PermitLimit.Should().Be(permitLimit);
        exception.WindowSeconds.Should().Be(windowSeconds);
        exception.RetryAfterSeconds.Should().Be(retryAfterSeconds);
        exception.ResetTimestamp.Should().Be(resetTimestamp);
    }

    [Fact]
    public void Constructor_ShouldGenerateCorrectMessage()
    {
        // Arrange
        var policyName = "strict";
        var permitLimit = 50;
        var windowSeconds = 60;
        var retryAfterSeconds = 45;
        var resetTimestamp = DateTimeOffset.UtcNow.AddSeconds(45).ToUnixTimeSeconds();

        // Act
        var exception = new RateLimitExceededException(
            policyName,
            permitLimit,
            windowSeconds,
            retryAfterSeconds,
            resetTimestamp);

        // Assert
        exception.Message.Should().Contain("Rate limit exceeded");
        exception.Message.Should().Contain(policyName);
        exception.Message.Should().Contain(permitLimit.ToString());
        exception.Message.Should().Contain(windowSeconds.ToString());
        exception.Message.Should().Contain(retryAfterSeconds.ToString());
    }

    [Fact]
    public void Message_ShouldDescribeRateLimit()
    {
        // Arrange & Act
        var exception = new RateLimitExceededException(
            "default",
            100,
            60,
            30,
            DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds());

        // Assert
        exception.Message.Should().Be(
            "Rate limit exceeded for policy 'default'. Limit: 100 requests per 60 seconds. Retry after 30 seconds.");
    }

    [Theory]
    [InlineData("default", 250, 60)]
    [InlineData("strict", 50, 60)]
    [InlineData("relaxed", 500, 60)]
    public void Constructor_WithVariousPolicies_ShouldSetCorrectly(
        string policy, int limit, int window)
    {
        // Arrange
        var retryAfter = 30;
        var resetTimestamp = DateTimeOffset.UtcNow.AddSeconds(retryAfter).ToUnixTimeSeconds();

        // Act
        var exception = new RateLimitExceededException(
            policy,
            limit,
            window,
            retryAfter,
            resetTimestamp);

        // Assert
        exception.PolicyName.Should().Be(policy);
        exception.PermitLimit.Should().Be(limit);
        exception.WindowSeconds.Should().Be(window);
    }

    [Fact]
    public void Exception_ShouldBeThrowable()
    {
        // Arrange
        var exception = new RateLimitExceededException(
            "default",
            100,
            60,
            30,
            DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds());

        // Act & Assert
        Action act = () => throw exception;
        act.Should().Throw<RateLimitExceededException>();
    }
}
