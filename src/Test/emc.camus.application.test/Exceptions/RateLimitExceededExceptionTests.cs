using FluentAssertions;
using emc.camus.application.Exceptions;

namespace emc.camus.application.test.Exceptions;

public class RateLimitExceededExceptionTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsPropertiesAndMessage()
    {
        // Arrange
        var policyName = "strict";
        var permitLimit = 10;
        var windowSeconds = 60;
        var retryAfterSeconds = 30;
        var resetTimestamp = 1700000000L;

        // Act
        var exception = new RateLimitExceededException(
            policyName, permitLimit, windowSeconds,
            retryAfterSeconds, resetTimestamp);

        // Assert
        exception.PolicyName.Should().Be(policyName);
        exception.PermitLimit.Should().Be(permitLimit);
        exception.WindowSeconds.Should().Be(windowSeconds);
        exception.RetryAfterSeconds.Should().Be(retryAfterSeconds);
        exception.ResetTimestamp.Should().Be(resetTimestamp);
        exception.Message.Should().Match($"*{policyName}*{permitLimit}*{windowSeconds}*{retryAfterSeconds}*");
    }
}
