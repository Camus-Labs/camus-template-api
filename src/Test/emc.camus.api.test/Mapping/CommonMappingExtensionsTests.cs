using FluentAssertions;
using emc.camus.api.Mapping;
using emc.camus.api.Models.Requests;
using emc.camus.application.Common;

namespace emc.camus.api.test.Mapping;

public class CommonMappingExtensionsTests
{
    // --- ToPaginationParams ---

    [Fact]
    public void ToPaginationParams_ValidQuery_MapsCorrectly()
    {
        // Arrange
        var query = new PaginationQuery { Page = 3, PageSize = 50 };

        // Act
        var result = query.ToPaginationParams();

        // Assert
        result.Page.Should().Be(3);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public void ToPaginationParams_DefaultQuery_MapsDefaults()
    {
        // Arrange
        var query = new PaginationQuery();

        // Act
        var result = query.ToPaginationParams();

        // Assert
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(25);
    }

    // --- ToPagedResponse ---

    [Fact]
    public void ToPagedResponse_ValidPagedResult_MapsAllProperties()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };
        var pagedResult = new PagedResult<int>(items, 10, 1, 3);

        // Act
        var response = pagedResult.ToPagedResponse(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        response.Items.Should().BeEquivalentTo(new List<string> { "1", "2", "3" });
        response.TotalCount.Should().Be(10);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(3);
        response.TotalPages.Should().Be(4);
        response.HasNextPage.Should().BeTrue();
        response.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void ToPagedResponse_LastPage_HasNextPageIsFalse()
    {
        // Arrange
        var items = new List<string> { "item" };
        var pagedResult = new PagedResult<string>(items, 3, 3, 1);

        // Act
        var response = pagedResult.ToPagedResponse(x => x.ToUpperInvariant());

        // Assert
        response.HasNextPage.Should().BeFalse();
        response.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void ToPagedResponse_EmptyItems_MapsToEmptyList()
    {
        // Arrange
        var pagedResult = new PagedResult<int>(new List<int>(), 0, 1, 10);

        // Act
        var response = pagedResult.ToPagedResponse(x => x * 2);

        // Assert
        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
    }
}
