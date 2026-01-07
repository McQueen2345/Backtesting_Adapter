using Xunit;
using QTS.Backtest.Adapter.Enums;
using QTS.Backtest.Adapter.Models;

namespace QTS.Backtest.Adapter.Tests.Models;

public class TradePrintEventTests
{
    [Fact]
    public void CanCreate_WithValidData_SetsAllProperties()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var trade = new TradePrintEvent
        {
            Timestamp = timestamp,
            RowIndex = 100,
            Price = 4525.50m,
            Size = 10,
            Aggressor = Side.Bid
        };

        // Assert
        Assert.Equal(timestamp, trade.Timestamp);
        Assert.Equal(100, trade.RowIndex);
        Assert.Equal(4525.50m, trade.Price);
        Assert.Equal(10, trade.Size);
        Assert.Equal(Side.Bid, trade.Aggressor);
    }

    [Fact]
    public void Aggressor_Bid_MeansBuyerAggressive()
    {
        // Arrange & Act
        // When Aggressor = Bid, it means the buyer initiated the trade
        // (market buy order hitting the ask side)
        var trade = new TradePrintEvent
        {
            Aggressor = Side.Bid
        };

        // Assert
        Assert.Equal(Side.Bid, trade.Aggressor);
        // Semantically: Buyer was aggressive, lifted the offer
    }

    [Fact]
    public void Aggressor_Ask_MeansSellerAggressive()
    {
        // Arrange & Act
        // When Aggressor = Ask, it means the seller initiated the trade
        // (market sell order hitting the bid side)
        var trade = new TradePrintEvent
        {
            Aggressor = Side.Ask
        };

        // Assert
        Assert.Equal(Side.Ask, trade.Aggressor);
        // Semantically: Seller was aggressive, hit the bid
    }

    // NOTE: Timestamp.Kind == DateTimeKind.Utc validation will be enforced in the parser.
    // The model itself allows any DateTimeKind for flexibility, but the parser
    // MUST ensure UTC timestamps are used.
    //
    // Test documentation: Timestamp_ShouldBeUtc
    // The actual enforcement happens at parse time, not at model creation time.
    // This keeps the model simple and puts validation where data enters the system.
}
