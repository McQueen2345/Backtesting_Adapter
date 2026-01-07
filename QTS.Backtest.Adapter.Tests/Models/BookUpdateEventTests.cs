using Xunit;
using QTS.Backtest.Adapter.Enums;
using QTS.Backtest.Adapter.Models;

namespace QTS.Backtest.Adapter.Tests.Models;

public class BookUpdateEventTests
{
    #region ShouldRemoveLevel Tests - RED LIST Implementation

    [Fact]
    public void ShouldRemoveLevel_WhenDelete_ReturnsTrue()
    {
        // Arrange
        var evt = new BookUpdateEvent
        {
            Operation = Operation.Delete,
            Size = 100 // Positive size, but Delete operation
        };

        // Act & Assert
        Assert.True(evt.ShouldRemoveLevel);
    }

    [Fact]
    public void ShouldRemoveLevel_WhenSizeZero_ReturnsTrue()
    {
        // Arrange
        var evt = new BookUpdateEvent
        {
            Operation = Operation.Update,
            Size = 0
        };

        // Act & Assert
        Assert.True(evt.ShouldRemoveLevel);
    }

    [Fact]
    public void ShouldRemoveLevel_WhenSizeNegative_ReturnsTrue()
    {
        // Arrange
        var evt = new BookUpdateEvent
        {
            Operation = Operation.Update,
            Size = -1
        };

        // Act & Assert
        Assert.True(evt.ShouldRemoveLevel);
    }

    [Fact]
    public void ShouldRemoveLevel_WhenAddWithPositiveSize_ReturnsFalse()
    {
        // Arrange
        var evt = new BookUpdateEvent
        {
            Operation = Operation.Add,
            Size = 100
        };

        // Act & Assert
        Assert.False(evt.ShouldRemoveLevel);
    }

    [Fact]
    public void ShouldRemoveLevel_WhenUpdateWithPositiveSize_ReturnsFalse()
    {
        // Arrange
        var evt = new BookUpdateEvent
        {
            Operation = Operation.Update,
            Size = 50
        };

        // Act & Assert
        Assert.False(evt.ShouldRemoveLevel);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var evt = new BookUpdateEvent
        {
            Timestamp = timestamp,
            RowIndex = 42,
            Mdt = 66, // 'B' for Bid
            Side = Side.Bid,
            Depth = 5,
            Operation = Operation.Add,
            Price = 100.50m,
            Size = 1000,
            Level = 3
        };

        // Assert
        Assert.Equal(timestamp, evt.Timestamp);
        Assert.Equal(42, evt.RowIndex);
        Assert.Equal(66, evt.Mdt);
        Assert.Equal(Side.Bid, evt.Side);
        Assert.Equal(5, evt.Depth);
        Assert.Equal(Operation.Add, evt.Operation);
        Assert.Equal(100.50m, evt.Price);
        Assert.Equal(1000, evt.Size);
        Assert.Equal(3, evt.Level);
    }

    [Fact]
    public void Level_CanBeNull()
    {
        // Arrange & Act
        var evt = new BookUpdateEvent
        {
            Level = null
        };

        // Assert
        Assert.Null(evt.Level);
    }

    // NOTE: Timestamp.Kind == DateTimeKind.Utc validation will be enforced in the parser.
    // The model itself allows any DateTimeKind for flexibility, but the parser
    // MUST ensure UTC timestamps are used.

    #endregion
}
