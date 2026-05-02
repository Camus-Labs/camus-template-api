using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.application.Common;

namespace emc.camus.application.test.Auth;

public class GeneratedTokenSortParamsTests
{
    [Theory]
    [InlineData("createdAt", "desc", GeneratedTokenSortField.CreatedAt, SortDirection.Desc)]
    [InlineData("expiresOn", "asc", GeneratedTokenSortField.ExpiresOn, SortDirection.Asc)]
    [InlineData("tokenUsername", "asc", GeneratedTokenSortField.TokenUsername, SortDirection.Asc)]
    [InlineData("revokedAt", "desc", GeneratedTokenSortField.RevokedAt, SortDirection.Desc)]
    public void Constructor_ValidStrings_ParsesFieldAndDirection(
        string sortBy, string sortDirection, GeneratedTokenSortField expectedField, SortDirection expectedDirection)
    {
        // Arrange
        // (inputs from [InlineData])

        // Act
        var sortParams = new GeneratedTokenSortParams(sortBy, sortDirection);

        // Assert
        sortParams.Field.Should().Be(expectedField);
        sortParams.Direction.Should().Be(expectedDirection);
    }

    [Theory]
    [InlineData("invalidField", "asc", "*sortBy*")]
    [InlineData("createdAt", "invalid", "*sortDirection*")]
    public void Constructor_InvalidValue_ThrowsArgumentException(string sortBy, string sortDirection, string expectedMessagePattern)
    {
        // Arrange
        // (inputs from [InlineData])

        // Act
        var act = () => new GeneratedTokenSortParams(sortBy, sortDirection);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage(expectedMessagePattern);
    }

    [Theory]
    [InlineData("createdAt", null)]
    [InlineData(null, "asc")]
    public void Constructor_OnlyOneProvided_ThrowsArgumentException(string? sortBy, string? sortDirection)
    {
        // Arrange
        // (inputs from [InlineData])

        // Act
        var act = () => new GeneratedTokenSortParams(sortBy, sortDirection);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*sortBy*sortDirection*");
    }

    // --- Both null ---

    [Fact]
    public void Constructor_BothNull_ReturnsInstanceWithNoSorting()
    {
        // Arrange
        // (no inputs — default constructor)

        // Act
        var sortParams = new GeneratedTokenSortParams();

        // Assert
        sortParams.Field.Should().BeNull();
        sortParams.Direction.Should().BeNull();
    }
}
