using FluentAssertions;
using emc.camus.application.Auth;
using emc.camus.application.Common;

namespace emc.camus.application.test.Auth;

public class GeneratedTokenSortParamsTests
{
    [Theory]
    [InlineData(GeneratedTokenSortField.CreatedAt, SortDirection.Desc)]
    [InlineData(GeneratedTokenSortField.ExpiresOn, SortDirection.Asc)]
    [InlineData(GeneratedTokenSortField.TokenUsername, SortDirection.Asc)]
    [InlineData(GeneratedTokenSortField.RevokedAt, SortDirection.Desc)]
    public void Constructor_ValidFieldAndDirection_SetsProperties(GeneratedTokenSortField field, SortDirection direction)
    {
        // Arrange & Act
        var sortParams = new GeneratedTokenSortParams(field, direction);

        // Assert
        sortParams.Field.Should().Be(field);
        sortParams.Direction.Should().Be(direction);
    }
}
