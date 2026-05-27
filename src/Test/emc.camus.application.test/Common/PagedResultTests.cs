using FluentAssertions;
using emc.camus.application.Common;

namespace emc.camus.application.test.Common;

public class PagedResultTests
{
    private const int DefaultTotalCount = 50;
    private const int DefaultPage = 1;
    private const int SecondPage = 2;
    private const int DefaultPageSize = 25;
    private static readonly IReadOnlyList<string> EmptyItems = [];
    private static readonly IReadOnlyList<string> ValidItems = ["item1", "item2"];

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var items = ValidItems.ToList();
        var totalCount = DefaultTotalCount;
        var page = DefaultPage;
        var pageSize = DefaultPageSize;

        // Act
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Assert
        result.Items.Should().BeEquivalentTo(ValidItems);
        result.TotalCount.Should().Be(totalCount);
        result.Page.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public void Constructor_NullItems_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new PagedResult<string>(null!, DefaultTotalCount, DefaultPage, DefaultPageSize);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("items");
    }

    [Fact]
    public void Constructor_NegativeTotalCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        var act = () => new PagedResult<string>(EmptyItems.ToList(), -1, DefaultPage, DefaultPageSize);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("totalCount");
    }

    [Fact]
    public void Constructor_PageLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        var act = () => new PagedResult<string>(EmptyItems.ToList(), DefaultTotalCount, 0, DefaultPageSize);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("page");
    }

    [Fact]
    public void Constructor_PageSizeLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        var act = () => new PagedResult<string>(EmptyItems.ToList(), DefaultTotalCount, DefaultPage, 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("pageSize");
    }

    // --- TotalPages ---

    [Theory]
    [InlineData(DefaultTotalCount, DefaultPageSize, 2)]
    [InlineData(51, DefaultPageSize, 3)]
    [InlineData(0, DefaultPageSize, 0)]
    [InlineData(1, DefaultPageSize, 1)]
    [InlineData(100, 10, 10)]
    public void TotalPages_VariousCounts_ReturnsCorrectValue(int totalCount, int pageSize, int expectedTotalPages)
    {
        // Arrange
        var result = new PagedResult<string>(EmptyItems.ToList(), totalCount, DefaultPage, pageSize);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        totalPages.Should().Be(expectedTotalPages);
    }

    // --- HasNextPage ---

    [Theory]
    [InlineData(DefaultTotalCount, DefaultPage, DefaultPageSize, true)]
    [InlineData(DefaultTotalCount, SecondPage, DefaultPageSize, false)]
    public void HasNextPage_VariousPages_ReturnsExpectedResult(int totalCount, int page, int pageSize, bool expected)
    {
        // Arrange
        var result = new PagedResult<string>(EmptyItems.ToList(), totalCount, page, pageSize);

        // Act
        var hasNext = result.HasNextPage;

        // Assert
        hasNext.Should().Be(expected);
    }

    // --- HasPreviousPage ---

    [Theory]
    [InlineData(DefaultTotalCount, DefaultPage, DefaultPageSize, false)]
    [InlineData(DefaultTotalCount, SecondPage, DefaultPageSize, true)]
    public void HasPreviousPage_VariousPages_ReturnsExpectedResult(int totalCount, int page, int pageSize, bool expected)
    {
        // Arrange
        var result = new PagedResult<string>(EmptyItems.ToList(), totalCount, page, pageSize);

        // Act
        var hasPrevious = result.HasPreviousPage;

        // Assert
        hasPrevious.Should().Be(expected);
    }
}
