using Xunit;
using QTS.Backtest.Adapter.Enums;

namespace QTS.Backtest.Adapter.Tests.Enums;

public class AdapterEnumsTests
{
    #region Side Enum Tests

    [Fact]
    public void Side_ShouldHaveBidValue()
    {
        // Arrange & Act
        var side = Side.Bid;

        // Assert
        Assert.Equal(Side.Bid, side);
    }

    [Fact]
    public void Side_ShouldHaveAskValue()
    {
        // Arrange & Act
        var side = Side.Ask;

        // Assert
        Assert.Equal(Side.Ask, side);
    }

    [Fact]
    public void Side_Bid_CastToInt_ShouldBeZero()
    {
        // Arrange & Act
        var value = (int)Side.Bid;

        // Assert
        Assert.Equal(0, value);
    }

    [Fact]
    public void Side_Ask_CastToInt_ShouldBeOne()
    {
        // Arrange & Act
        var value = (int)Side.Ask;

        // Assert
        Assert.Equal(1, value);
    }

    [Fact]
    public void Side_Bid_ToString_ShouldReturnBid()
    {
        // Arrange & Act
        var result = Side.Bid.ToString();

        // Assert
        Assert.Equal("Bid", result);
    }

    [Fact]
    public void Side_Ask_ToString_ShouldReturnAsk()
    {
        // Arrange & Act
        var result = Side.Ask.ToString();

        // Assert
        Assert.Equal("Ask", result);
    }

    #endregion

    #region Operation Enum Tests

    [Fact]
    public void Operation_Add_ShouldHaveValueZero()
    {
        // Arrange & Act
        var value = (int)Operation.Add;

        // Assert
        Assert.Equal(0, value);
    }

    [Fact]
    public void Operation_Update_ShouldHaveValueOne()
    {
        // Arrange & Act
        var value = (int)Operation.Update;

        // Assert
        Assert.Equal(1, value);
    }

    [Fact]
    public void Operation_Delete_ShouldHaveValueTwo()
    {
        // Arrange & Act
        var value = (int)Operation.Delete;

        // Assert
        Assert.Equal(2, value);
    }

    [Fact]
    public void Operation_Add_ToString_ShouldReturnAdd()
    {
        // Arrange & Act
        var result = Operation.Add.ToString();

        // Assert
        Assert.Equal("Add", result);
    }

    [Fact]
    public void Operation_Update_ToString_ShouldReturnUpdate()
    {
        // Arrange & Act
        var result = Operation.Update.ToString();

        // Assert
        Assert.Equal("Update", result);
    }

    [Fact]
    public void Operation_Delete_ToString_ShouldReturnDelete()
    {
        // Arrange & Act
        var result = Operation.Delete.ToString();

        // Assert
        Assert.Equal("Delete", result);
    }

    #endregion
}
