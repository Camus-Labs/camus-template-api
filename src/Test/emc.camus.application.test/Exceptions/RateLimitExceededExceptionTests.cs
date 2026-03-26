using System.Globalization;
using FluentAssertions;
using emc.camus.application.Exceptions;

namespace emc.camus.application.test.Exceptions;

public class RateLimitExceededExceptionTests
{
    private const string ValidPolicyName = "strict";
    private const int ValidPermitLimit = 10;
    private const int ValidWindowSeconds = 60;
    private const int ValidRetryAfterSeconds = 30;
    private const long ValidResetTimestamp = 1700000000;

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsPropertiesAndMessage()
    {
        // Arrange
        // Act
        var exception = new RateLimitExceededException(
            ValidPolicyName, ValidPermitLimit, ValidWindowSeconds,
            ValidRetryAfterSeconds, ValidResetTimestamp);

        // Assert
        exception.PolicyName.Should().Be(ValidPolicyName);
        exception.PermitLimit.Should().Be(ValidPermitLimit);
        exception.WindowSeconds.Should().Be(ValidWindowSeconds);
        exception.RetryAfterSeconds.Should().Be(ValidRetryAfterSeconds);
        exception.ResetTimestamp.Should().Be(ValidResetTimestamp);
        exception.Message.Should().Contain(ValidPolicyName);
        exception.Message.Should().Contain(ValidPermitLimit.ToString(CultureInfo.InvariantCulture));
        exception.Message.Should().Contain(ValidWindowSeconds.ToString(CultureInfo.InvariantCulture));
        exception.Message.Should().Contain(ValidRetryAfterSeconds.ToString(CultureInfo.InvariantCulture));
    }
}
