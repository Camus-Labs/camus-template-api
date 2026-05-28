using FluentAssertions;
using emc.camus.application.Common;

namespace emc.camus.application.test.Common;

public class PaginationParamsTests
{
    private const int MinClampValue = 1;
    private const int ValidPage = 3;
    private const int ValidPageSize = 50;
    private const int TestPageSize = 25;
    private const int MaxPageSize = 100;

    // --- Constructor ---

    [Fact]
    public void Constructor_DefaultParameters_SetsDefaults()
    {
        // Arrange
        // Act
        var pagination = new PaginationParams();

        // Assert
        pagination.Page.Should().Be(MinClampValue);
        pagination.PageSize.Should().Be(PaginationParams.DefaultPageSize);
    }

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var page = ValidPage;
        var pageSize = ValidPageSize;

        // Act
        var pagination = new PaginationParams(page, pageSize);

        // Assert
        pagination.Page.Should().Be(ValidPage);
        pagination.PageSize.Should().Be(ValidPageSize);
    }

    [Theory]
    [InlineData(0, MinClampValue)]
    [InlineData(-5, MinClampValue)]
    [InlineData(-100, MinClampValue)]
    public void Constructor_PageBelowMinimum_ClampsToOne(int page, int expectedPage)
    {
        // Arrange
        // Act
        var pagination = new PaginationParams(page);

        // Assert
        pagination.Page.Should().Be(expectedPage);
    }

    [Theory]
    [InlineData(0, MinClampValue)]
    [InlineData(-10, MinClampValue)]
    public void Constructor_PageSizeBelowMinimum_ClampsToOne(int pageSize, int expectedPageSize)
    {
        // Arrange
        // Act
        var pagination = new PaginationParams(pageSize: pageSize);

        // Assert
        pagination.PageSize.Should().Be(expectedPageSize);
    }

    [Theory]
    [InlineData(101, MaxPageSize)]
    [InlineData(500, MaxPageSize)]
    [InlineData(int.MaxValue, MaxPageSize)]
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
    [InlineData(1, TestPageSize, 0)]
    [InlineData(2, TestPageSize, TestPageSize)]
    [InlineData(ValidPage, 10, 20)]
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
