using FluentAssertions;
using emc.camus.application.Common;

namespace emc.camus.application.test.Common;

public class PaginationParamsTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_DefaultParameters_SetsDefaults()
    {
        // Arrange
        // Act
        var pagination = new PaginationParams();

        // Assert
        pagination.Page.Should().Be(1);
        pagination.PageSize.Should().Be(PaginationParams.DefaultPageSize);
    }

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var page = 3;
        var pageSize = 50;

        // Act
        var pagination = new PaginationParams(page, pageSize);

        // Assert
        pagination.Page.Should().Be(3);
        pagination.PageSize.Should().Be(50);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(-100, 1)]
    public void Constructor_PageBelowMinimum_ClampsToOne(int page, int expectedPage)
    {
        // Arrange
        // Act
        var pagination = new PaginationParams(page);

        // Assert
        pagination.Page.Should().Be(expectedPage);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    public void Constructor_PageSizeBelowMinimum_ClampsToOne(int pageSize, int expectedPageSize)
    {
        // Arrange
        // Act
        var pagination = new PaginationParams(pageSize: pageSize);

        // Assert
        pagination.PageSize.Should().Be(expectedPageSize);
    }

    [Theory]
    [InlineData(101, 100)]
    [InlineData(500, 100)]
    [InlineData(int.MaxValue, 100)]
    public void Constructor_PageSizeAboveMaximum_ClampsToMax(int pageSize, int expectedPageSize)
    {
        // Arrange
        // Act
        var pagination = new PaginationParams(pageSize: pageSize);

        // Assert
        pagination.PageSize.Should().Be(expectedPageSize);
    }

    // --- Offset ---

    [Theory]
    [InlineData(1, 25, 0)]
    [InlineData(2, 25, 25)]
    [InlineData(3, 10, 20)]
    public void Offset_VariousPages_ReturnsCorrectOffset(int page, int pageSize, int expectedOffset)
    {
        // Arrange
        var pagination = new PaginationParams(page, pageSize);

        // Act
        var offset = pagination.Offset;

        // Assert
        offset.Should().Be(expectedOffset);
    }
}
