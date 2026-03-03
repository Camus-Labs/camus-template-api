using emc.camus.application.Common;
using FluentAssertions;

namespace emc.camus.application.test.Common;

/// <summary>
/// Unit tests for the PagedResult generic type.
/// </summary>
public class PagedResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };

        // Act
        var result = new PagedResult<string>(items, 10, 1, 3);

        // Assert
        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
    }

    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var result = new PagedResult<int>(new List<int> { 1, 2, 3 }, 10, 1, 3);

        // Assert
        result.TotalPages.Should().Be(4); // ceil(10/3) = 4
    }

    [Fact]
    public void TotalPages_WithExactDivision_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var result = new PagedResult<int>(new List<int> { 1, 2, 3 }, 9, 1, 3);

        // Assert
        result.TotalPages.Should().Be(3); // ceil(9/3) = 3
    }

    [Fact]
    public void HasNextPage_OnFirstPage_WithMultiplePages_ShouldBeTrue()
    {
        // Arrange & Act
        var result = new PagedResult<int>(new List<int> { 1, 2, 3 }, 10, 1, 3);

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnLastPage_ShouldBeFalse()
    {
        // Arrange & Act
        var result = new PagedResult<int>(new List<int> { 10 }, 10, 4, 3);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnFirstPage_ShouldBeFalse()
    {
        // Arrange & Act
        var result = new PagedResult<int>(new List<int> { 1, 2, 3 }, 10, 1, 3);

        // Assert
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ShouldBeTrue()
    {
        // Arrange & Act
        var result = new PagedResult<int>(new List<int> { 4, 5, 6 }, 10, 2, 3);

        // Assert
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Empty_ShouldReturnEmptyResult()
    {
        // Arrange & Act
        var result = PagedResult<string>.Empty(2, 10);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue(); // Page 2 has a previous page conceptually
    }

    [Fact]
    public void Empty_WithDefaults_ShouldUseDefaults()
    {
        // Arrange & Act
        var result = PagedResult<string>.Empty();

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(25);
    }

    [Fact]
    public void Constructor_WithNullItems_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => new PagedResult<string>(null!, 10, 1, 3);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNegativeTotalCount_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => new PagedResult<string>(new List<string>(), -1, 1, 3);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithZeroPage_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => new PagedResult<string>(new List<string>(), 10, 0, 3);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithZeroPageSize_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => new PagedResult<string>(new List<string>(), 10, 1, 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SinglePage_ShouldHaveNoNextOrPrevious()
    {
        // Arrange & Act
        var result = new PagedResult<int>(new List<int> { 1, 2 }, 2, 1, 25);

        // Assert
        result.TotalPages.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }
}
