using FluentAssertions;
using emc.camus.application.Common;

namespace emc.camus.application.test.Common;

public class PagedResultTests
{
    private const int DefaultTotalCount = 50;
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 25;

    // --- Constructor ---

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };
        var totalCount = DefaultTotalCount;
        var page = DefaultPage;
        var pageSize = DefaultPageSize;

        // Act
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Assert
        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(totalCount);
        result.Page.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public void Constructor_NullItems_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        var act = () => new PagedResult<string>(null!, 10, 1, 25);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("items");
    }

    [Fact]
    public void Constructor_NegativeTotalCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        var act = () => new PagedResult<string>([], -1, 1, 25);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("totalCount");
    }

    [Fact]
    public void Constructor_PageLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        var act = () => new PagedResult<string>([], 10, 0, 25);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("page");
    }

    [Fact]
    public void Constructor_PageSizeLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // Act
        var act = () => new PagedResult<string>([], 10, 1, 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("pageSize");
    }

    // --- TotalPages ---

    [Theory]
    [InlineData(50, 25, 2)]
    [InlineData(51, 25, 3)]
    [InlineData(0, 25, 0)]
    [InlineData(1, 25, 1)]
    [InlineData(100, 10, 10)]
    public void TotalPages_VariousCounts_ReturnsCorrectValue(int totalCount, int pageSize, int expectedTotalPages)
    {
        // Arrange
        var result = new PagedResult<string>([], totalCount, 1, pageSize);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        totalPages.Should().Be(expectedTotalPages);
    }

    // --- HasNextPage ---

    [Theory]
    [InlineData(DefaultTotalCount, 1, DefaultPageSize, true)]
    [InlineData(DefaultTotalCount, 2, DefaultPageSize, false)]
    public void HasNextPage_VariousPages_ReturnsExpectedResult(int totalCount, int page, int pageSize, bool expected)
    {
        // Arrange
        var result = new PagedResult<string>([], totalCount, page, pageSize);

        // Act
        var hasNext = result.HasNextPage;

        // Assert
        hasNext.Should().Be(expected);
    }

    // --- HasPreviousPage ---

    [Theory]
    [InlineData(DefaultTotalCount, 1, DefaultPageSize, false)]
    [InlineData(DefaultTotalCount, 2, DefaultPageSize, true)]
    public void HasPreviousPage_VariousPages_ReturnsExpectedResult(int totalCount, int page, int pageSize, bool expected)
    {
        // Arrange
        var result = new PagedResult<string>([], totalCount, page, pageSize);

        // Act
        var hasPrevious = result.HasPreviousPage;

        // Assert
        hasPrevious.Should().Be(expected);
    }
}
