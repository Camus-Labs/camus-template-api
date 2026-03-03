using emc.camus.application.Common;
using FluentAssertions;

namespace emc.camus.application.test.Common;

/// <summary>
/// Unit tests for PaginationParams.
/// </summary>
public class PaginationParamsTests
{
    [Fact]
    public void Constructor_WithDefaults_ShouldBePageOneAndSize25()
    {
        // Arrange & Act
        var pagination = new PaginationParams();

        // Assert
        pagination.Page.Should().Be(1);
        pagination.PageSize.Should().Be(PaginationParams.DefaultPageSize);
    }

    [Fact]
    public void Constructor_WithValidValues_ShouldSetCorrectly()
    {
        // Arrange & Act
        var pagination = new PaginationParams(3, 10);

        // Assert
        pagination.Page.Should().Be(3);
        pagination.PageSize.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithNegativePage_ShouldClampToOne()
    {
        // Arrange & Act
        var pagination = new PaginationParams(-5, 10);

        // Assert
        pagination.Page.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithZeroPage_ShouldClampToOne()
    {
        // Arrange & Act
        var pagination = new PaginationParams(0, 10);

        // Assert
        pagination.Page.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithPageSizeAboveMax_ShouldClampToMax()
    {
        // Arrange & Act
        var pagination = new PaginationParams(1, 500);

        // Assert
        pagination.PageSize.Should().Be(PaginationParams.MaxPageSize);
    }

    [Fact]
    public void Constructor_WithZeroPageSize_ShouldClampToOne()
    {
        // Arrange & Act
        var pagination = new PaginationParams(1, 0);

        // Assert
        pagination.PageSize.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithNegativePageSize_ShouldClampToOne()
    {
        // Arrange & Act
        var pagination = new PaginationParams(1, -10);

        // Assert
        pagination.PageSize.Should().Be(1);
    }

    [Fact]
    public void Offset_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var pagination = new PaginationParams(3, 10);

        // Assert
        pagination.Offset.Should().Be(20); // (3 - 1) * 10
    }

    [Fact]
    public void Offset_OnFirstPage_ShouldBeZero()
    {
        // Arrange & Act
        var pagination = new PaginationParams(1, 25);

        // Assert
        pagination.Offset.Should().Be(0);
    }

    [Fact]
    public void MaxPageSize_ShouldBe100()
    {
        // Assert
        PaginationParams.MaxPageSize.Should().Be(100);
    }

    [Fact]
    public void DefaultPageSize_ShouldBe25()
    {
        // Assert
        PaginationParams.DefaultPageSize.Should().Be(25);
    }
}
