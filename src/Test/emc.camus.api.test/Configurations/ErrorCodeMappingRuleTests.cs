using FluentAssertions;
using emc.camus.api.Configurations;

namespace emc.camus.api.test.Configurations;

public class ErrorCodeMappingRuleTests
{
    // --- Validate: Valid Rules ---

    [Fact]
    public void Validate_TypeAndErrorCode_Succeeds()
    {
        // Arrange
        var rule = new ErrorCodeMappingRule
        {
            Type = "ArgumentException",
            ErrorCode = "bad_request"
        };

        // Act
        var act = () => rule.Validate(0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_PatternAndErrorCode_Succeeds()
    {
        // Arrange
        var rule = new ErrorCodeMappingRule
        {
            Pattern = "not.?found",
            ErrorCode = "not_found"
        };

        // Act
        var act = () => rule.Validate(0);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_TypePatternAndErrorCode_Succeeds()
    {
        // Arrange
        var rule = new ErrorCodeMappingRule
        {
            Type = "UnauthorizedAccessException",
            Pattern = "jwt.*expired",
            ErrorCode = "jwt_token_expired"
        };

        // Act
        var act = () => rule.Validate(0);

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
        var rule = new ErrorCodeMappingRule
        {
            Type = "ArgumentException",
            ErrorCode = errorCode!
        };

        // Act
        var act = () => rule.Validate(0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ErrorCode*null*empty*");
    }

    [Fact]
    public void Validate_ErrorCodeExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = new ErrorCodeMappingRule
        {
            Type = "ArgumentException",
            ErrorCode = new string('a', 51)
        };

        // Act
        var act = () => rule.Validate(0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ErrorCode*must not exceed*50*");
    }

    // --- Validate: TypeOrPattern Validation ---

    [Fact]
    public void Validate_NeitherTypeNorPattern_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = new ErrorCodeMappingRule
        {
            ErrorCode = "some_error"
        };

        // Act
        var act = () => rule.Validate(0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have either Type or Pattern*");
    }

    // --- Validate: Type Validation ---

    [Fact]
    public void Validate_TypeExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = new ErrorCodeMappingRule
        {
            Type = new string('a', 101),
            ErrorCode = "some_error"
        };

        // Act
        var act = () => rule.Validate(0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Type*must not exceed*100*");
    }

    // --- Validate: Pattern Validation ---

    [Fact]
    public void Validate_PatternExceedsMaxLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = new ErrorCodeMappingRule
        {
            Pattern = new string('a', 501),
            ErrorCode = "some_error"
        };

        // Act
        var act = () => rule.Validate(0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Pattern*must not exceed*500*");
    }

    // --- Validate: Index Validation ---

    [Fact]
    public void Validate_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var rule = new ErrorCodeMappingRule
        {
            Type = "ArgumentException",
            ErrorCode = "bad_request"
        };

        // Act
        var act = () => rule.Validate(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
