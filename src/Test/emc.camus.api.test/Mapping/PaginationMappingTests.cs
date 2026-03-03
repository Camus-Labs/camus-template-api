using emc.camus.api.Mapping;
using emc.camus.api.Mapping.V2;
using emc.camus.api.Models.Dtos.V2;
using emc.camus.api.Models.Requests;
using emc.camus.api.Models.Requests.V2;
using emc.camus.api.Models.Responses;
using emc.camus.application.Auth;
using emc.camus.application.Common;
using FluentAssertions;

namespace emc.camus.api.test.Mapping;

/// <summary>
/// Unit tests for pagination-related mapping extensions.
/// </summary>
public class PaginationMappingTests
{
    [Fact]
    public void ToPaginationParams_ShouldMapCorrectly()
    {
        // Arrange
        var query = new PaginationQuery { Page = 2, PageSize = 10 };

        // Act
        var result = query.ToPaginationParams();

        // Assert
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void ToPaginationParams_WithDefaults_ShouldUseDefaultValues()
    {
        // Arrange
        var query = new PaginationQuery();

        // Act
        var result = query.ToPaginationParams();

        // Assert
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(PaginationParams.DefaultPageSize);
    }

    [Fact]
    public void ToPaginationParams_WithNull_ShouldThrow()
    {
        // Arrange
        PaginationQuery? query = null;

        // Act
        Action act = () => query!.ToPaginationParams();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToPagedResponse_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var items = new List<GeneratedTokenSummaryView>
        {
            new(Guid.NewGuid(), "user-token", new List<string> { "read" },
                DateTime.UtcNow.AddDays(30), DateTime.UtcNow, false, null, true),
            new(Guid.NewGuid(), "user-token2", new List<string> { "read", "write" },
                DateTime.UtcNow.AddDays(30), DateTime.UtcNow, true, DateTime.UtcNow, false)
        };

        var pagedResult = new PagedResult<GeneratedTokenSummaryView>(items, 10, 2, 5);

        // Act
        var response = pagedResult.ToPagedResponse(r => r.ToDto());

        // Assert
        response.Items.Should().HaveCount(2);
        response.TotalCount.Should().Be(10);
        response.Page.Should().Be(2);
        response.PageSize.Should().Be(5);
        response.TotalPages.Should().Be(2);
        response.HasNextPage.Should().BeFalse();
        response.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void ToPagedResponse_WithEmptyResult_ShouldMapCorrectly()
    {
        // Arrange
        var pagedResult = PagedResult<GeneratedTokenSummaryView>.Empty(1, 25);

        // Act
        var response = pagedResult.ToPagedResponse(r => r.ToDto());

        // Assert
        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(25);
        response.TotalPages.Should().Be(0);
        response.HasNextPage.Should().BeFalse();
        response.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void ToPagedResponse_ShouldApplyMapperToEachItem()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };
        var pagedResult = new PagedResult<int>(items, 3, 1, 10);

        // Act
        var response = pagedResult.ToPagedResponse(i => i.ToString());

        // Assert
        response.Items.Should().BeEquivalentTo(new List<string> { "1", "2", "3" });
    }

    [Fact]
    public void ToPagedResponse_WithNullPagedResult_ShouldThrow()
    {
        // Arrange
        PagedResult<string>? pagedResult = null;

        // Act
        Action act = () => pagedResult!.ToPagedResponse(s => s);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToPagedResponse_WithNullMapper_ShouldThrow()
    {
        // Arrange
        var pagedResult = new PagedResult<string>(new List<string> { "a" }, 1, 1, 10);

        // Act
        Action act = () => pagedResult.ToPagedResponse<string, string>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToFilter_ShouldMapAllFilterProperties()
    {
        // Arrange
        var query = new GetGeneratedTokensQuery
        {
            ExcludeRevoked = true,
            ExcludeExpired = true
        };

        // Act
        var filter = query.ToFilter();

        // Assert
        filter.ExcludeRevoked.Should().BeTrue();
        filter.ExcludeExpired.Should().BeTrue();
    }

    [Fact]
    public void ToFilter_WithDefaults_ShouldHaveNoFilters()
    {
        // Arrange
        var query = new GetGeneratedTokensQuery();

        // Act
        var filter = query.ToFilter();

        // Assert
        filter.ExcludeRevoked.Should().BeFalse();
        filter.ExcludeExpired.Should().BeFalse();
    }

    [Fact]
    public void ToFilter_WithNull_ShouldThrow()
    {
        // Arrange
        GetGeneratedTokensQuery? query = null;

        // Act
        Action act = () => query!.ToFilter();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GeneratedTokenFilter_Defaults_ShouldHaveAllFiltersDisabled()
    {
        // Arrange & Act
        var filter = new GeneratedTokenFilter();

        // Assert
        filter.ExcludeRevoked.Should().BeFalse();
        filter.ExcludeExpired.Should().BeFalse();
    }
}
