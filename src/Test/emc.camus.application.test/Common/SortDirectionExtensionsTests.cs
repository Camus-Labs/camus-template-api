using FluentAssertions;
using emc.camus.application.Common;

namespace emc.camus.application.test.Common;

public class SortDirectionExtensionsTests
{
    [Fact]
    public void ToSql_Asc_ReturnsASC()
    {
        // Arrange
        var direction = SortDirection.Asc;

        // Act
        var result = direction.ToSql();

        // Assert
        result.Should().Be("ASC");
    }

    [Fact]
    public void ToSql_Desc_ReturnsDESC()
    {
        // Arrange
        var direction = SortDirection.Desc;

        // Act
        var result = direction.ToSql();

        // Assert
        result.Should().Be("DESC");
    }
}
