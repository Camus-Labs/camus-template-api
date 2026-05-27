using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.application.Common;

namespace emc.camus.application.test.Common;

public class SortParamsTests
{
    private const string SortByCreatedAt = "createdAt";
    private const string DirectionAsc = "asc";
    private const string DirectionDesc = "desc";

    [Theory]
    [InlineData(SortByCreatedAt, DirectionDesc, GeneratedTokenSortField.CreatedAt, SortDirection.Desc)]
    [InlineData("expiresOn", DirectionAsc, GeneratedTokenSortField.ExpiresOn, SortDirection.Asc)]
    [InlineData("tokenUsername", DirectionAsc, GeneratedTokenSortField.TokenUsername, SortDirection.Asc)]
    [InlineData("revokedAt", DirectionDesc, GeneratedTokenSortField.RevokedAt, SortDirection.Desc)]
    public void Constructor_ValidStrings_ParsesFieldAndDirection(
        string sortBy, string sortDirection, GeneratedTokenSortField expectedField, SortDirection expectedDirection)
    {
        // Arrange
        // (inputs from [InlineData])

        // Act
        var sortParams = new SortParams<GeneratedTokenSortField>(sortBy, sortDirection);

        // Assert
        sortParams.Field.Should().Be(expectedField);
        sortParams.Direction.Should().Be(expectedDirection);
    }

    [Theory]
    [InlineData("invalidField", DirectionAsc, "*sortBy*")]
    [InlineData(SortByCreatedAt, "invalid", "*sortDirection*")]
    public void Constructor_InvalidValue_ThrowsArgumentException(string sortBy, string sortDirection, string expectedMessagePattern)
    {
        // Arrange
        // (inputs from [InlineData])

        // Act
        var act = () => new SortParams<GeneratedTokenSortField>(sortBy, sortDirection);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage(expectedMessagePattern);
    }

    [Theory]
    [InlineData(SortByCreatedAt, null)]
    [InlineData(null, DirectionAsc)]
    public void Constructor_OnlyOneProvided_ThrowsArgumentException(string? sortBy, string? sortDirection)
    {
        // Arrange
        // (inputs from [InlineData])

        // Act
        var act = () => new SortParams<GeneratedTokenSortField>(sortBy, sortDirection);

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
        var sortParams = new SortParams<GeneratedTokenSortField>();

        // Assert
        sortParams.Field.Should().BeNull();
        sortParams.Direction.Should().BeNull();
    }
}
