using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class ErrorCodeMappingRuleSettingsTests
{
    // --- Validate: Valid Rules ---

    public static readonly TheoryData<ErrorCodeMappingRuleSettings> ValidRules = new()
    {
        new ErrorCodeMappingRuleSettings { Type = "ArgumentException", ErrorCode = "bad_request" },
        new ErrorCodeMappingRuleSettings { Pattern = "not.?found", ErrorCode = "not_found" },
        new ErrorCodeMappingRuleSettings { Type = "UnauthorizedAccessException", Pattern = "jwt.*expired", ErrorCode = "jwt_token_expired" }
    };

    [Theory]
    [MemberData(nameof(ValidRules))]
    public void Validate_ValidRule_Succeeds(ErrorCodeMappingRuleSettings rule)
    {
        // Arrange
        // Act
        var act = () => rule.Validate();

        // Assert
        act.Should().NotThrow();
    }

    // --- Validate: ErrorCode Validation ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyErrorCode_ThrowsInvalidOperationException(string? errorCode)
    {
        // Arrange
        var rule = new ErrorCodeMappingRuleSettings
        {
            Type = "ArgumentException",
            ErrorCode = errorCode!
        };

        // Act
        var act = () => rule.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ErrorCode*null*empty*");
    }

    [Fact]
    public void Validate_ErrorCodeExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = new ErrorCodeMappingRuleSettings
        {
            Type = "ArgumentException",
            ErrorCode = new string('a', 51)
        };

        // Act
        var act = () => rule.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ErrorCode*must not exceed*50*");
    }

    // --- Validate: TypeOrPattern Validation ---

    [Fact]
    public void Validate_NeitherTypeNorPattern_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = new ErrorCodeMappingRuleSettings
        {
            ErrorCode = "some_error"
        };

        // Act
        var act = () => rule.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have either Type or Pattern*");
    }

    // --- Validate: Type Validation ---

    [Fact]
    public void Validate_TypeExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = new ErrorCodeMappingRuleSettings
        {
            Type = new string('a', 101),
            ErrorCode = "some_error"
        };

        // Act
        var act = () => rule.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Type*must not exceed*100*");
    }

    // --- Validate: Pattern Validation ---

    [Fact]
    public void Validate_PatternExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = new ErrorCodeMappingRuleSettings
        {
            Pattern = new string('a', 501),
            ErrorCode = "some_error"
        };

        // Act
        var act = () => rule.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Pattern*must not exceed*500*");
    }

}
