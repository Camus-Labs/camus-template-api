using emc.camus.domain.Generic;
using FluentAssertions;

namespace emc.camus.domain.test.Generic;

/// <summary>
/// Unit tests for ApiResponse to verify response envelope behavior.
/// </summary>
public class ApiResponseTests
{
    [Fact]
    public void Constructor_ShouldInitializeTimestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var response = new ApiResponse<string>();

        var afterCreation = DateTime.UtcNow;

        // Assert
        response.Timestamp.Should().BeOnOrAfter(beforeCreation);
        response.Timestamp.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Constructor_ShouldUseUtcTime()
    {
        // Act
        var response = new ApiResponse<string>();

        // Assert
        response.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Message_ShouldBeSettable()
    {
        // Arrange
        var response = new ApiResponse<string>();
        var expectedMessage = "Operation successful";

        // Act
        response.Message = expectedMessage;

        // Assert
        response.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public void Data_ShouldBeSettable()
    {
        // Arrange
        var response = new ApiResponse<string>();
        var expectedData = "test data";

        // Act
        response.Data = expectedData;

        // Assert
        response.Data.Should().Be(expectedData);
    }

    [Fact]
    public void Data_WithComplexType_ShouldBeSettable()
    {
        // Arrange
        var response = new ApiResponse<TestData>();
        var expectedData = new TestData { Id = 1, Name = "Test" };

        // Act
        response.Data = expectedData;

        // Assert
        response.Data.Should().BeEquivalentTo(expectedData);
    }

    [Fact]
    public void Timestamp_ShouldBeSettable()
    {
        // Arrange
        var response = new ApiResponse<string>();
        var expectedTimestamp = DateTime.UtcNow.AddDays(-1);

        // Act
        response.Timestamp = expectedTimestamp;

        // Assert
        response.Timestamp.Should().Be(expectedTimestamp);
    }

    [Fact]
    public void Properties_ShouldDefaultToNull()
    {
        // Act
        var response = new ApiResponse<string>();

        // Assert
        response.Message.Should().BeNull();
        response.Data.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_WithGenericType_ShouldWork()
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

    [Fact]
    public void ApiResponse_WithNullableType_ShouldWork()
    {
        // Arrange & Act
        var response = new ApiResponse<int?>
        {
            Message = "No data",
            Data = null
        };

        // Assert
        response.Data.Should().BeNull();
        response.Message.Should().Be("No data");
    }

    private class TestData
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
