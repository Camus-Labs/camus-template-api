using emc.camus.domain.Generic;
using FluentAssertions;

namespace emc.camus.domain.test.Generic;

/// <summary>
/// Unit tests for ApiResponse to verify response envelope behavior.
/// Note: ApiResponse is marked with [ExcludeFromCodeCoverage] as it's a simple DTO.
/// These minimal tests verify only the critical timestamp initialization logic.
/// </summary>
public class ApiResponseTests
{
    [Fact]
    public void Constructor_ShouldInitializeTimestampWithUtc()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var response = new ApiResponse<string>();
        var afterCreation = DateTime.UtcNow;

        // Assert
        response.Timestamp.Should().BeOnOrAfter(beforeCreation);
        response.Timestamp.Should().BeOnOrBefore(afterCreation);
        response.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ApiResponse_WithGenericData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var response = new ApiResponse<int>
        {
            Message = "Success",
            Data = 42
        };

        // Assert
        response.Data.Should().Be(42);
        response.Message.Should().Be("Success");
    }
}
