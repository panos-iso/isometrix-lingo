using System;
using IsometrixLingo.Models;
using Xunit;

namespace IsometrixLingo.Tests.Models;

public class ConfirmationTests
{
    [Fact]
    public void ToFileFormat_FormatsCorrectly()
    {
        // Arrange
        var confirmation = new Confirmation
        {
            Username = "TestUser",
            Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var formatted = confirmation.ToFileFormat();

        // Assert
        Assert.Equal("CONFIRMED:by:[TestUser],at:[2024-01-15T10:30:00Z]", formatted);
    }

    [Fact]
    public void FromFileFormat_ParsesValidFormat()
    {
        // Arrange
        var formatted = "CONFIRMED:by:[JohnDoe],at:[2024-03-20T14:45:30Z]";

        // Act
        var confirmation = Confirmation.FromFileFormat(formatted);

        // Assert
        Assert.NotNull(confirmation);
        Assert.Equal("JohnDoe", confirmation.Username);
        Assert.Equal(new DateTime(2024, 3, 20, 14, 45, 30), confirmation.Timestamp.ToUniversalTime());
    }

    [Fact]
    public void FromFileFormat_ReturnsNullForInvalidFormat()
    {
        // Act
        var confirmation = Confirmation.FromFileFormat("INVALID");

        // Assert
        Assert.Null(confirmation);
    }

    [Fact]
    public void FromFileFormat_ReturnsNullForEmptyString()
    {
        // Act
        var confirmation = Confirmation.FromFileFormat("");

        // Assert
        Assert.Null(confirmation);
    }

    [Fact]
    public void FromFileFormat_ReturnsNullForNonConfirmedPrefix()
    {
        // Act
        var confirmation = Confirmation.FromFileFormat("SUGGESTION:by:[User],at:[2024-01-01T00:00:00Z]");

        // Assert
        Assert.Null(confirmation);
    }

    [Fact]
    public void DisplayText_FormatsReadably()
    {
        // Arrange
        var confirmation = new Confirmation
        {
            Username = "Alice",
            Timestamp = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var displayText = confirmation.DisplayText;

        // Assert
        Assert.Equal("Alice on Jun 15, 2024", displayText);
    }
}
