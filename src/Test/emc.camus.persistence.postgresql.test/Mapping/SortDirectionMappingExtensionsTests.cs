using FluentAssertions;
using emc.camus.application.Common;
using emc.camus.persistence.postgresql.Mapping;

namespace emc.camus.persistence.postgresql.test.Mapping;

public class SortDirectionMappingExtensionsTests
{
    [Theory]
    [InlineData(SortDirection.Asc, "ASC")]
    [InlineData(SortDirection.Desc, "DESC")]
    public void ToSql_ValidDirection_ReturnsExpectedSqlKeyword(SortDirection direction, string expected)
    {
        // Arrange
        // (no setup needed — inputs come from [InlineData])

        // Act
        var result = direction.ToSql();

        // Assert
        result.Should().Be(expected);
    }
}
