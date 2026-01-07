using Xunit;
using QTS.Backtest.Adapter.Enums;
using QTS.Backtest.Adapter.Mapping;

namespace QTS.Backtest.Adapter.Tests.Mapping;

public class MappingRegistryTests
{
    private readonly MappingRegistry _registry;

    public MappingRegistryTests()
    {
        _registry = new MappingRegistry();
    }

    #region TryMapMdtToSide Tests

    [Fact]
    public void TryMapMdtToSide_1_ReturnsBid()
    {
        // Act
        var result = _registry.TryMapMdtToSide(1);

        // Assert
        Assert.Equal(Side.Bid, result);
    }

    [Fact]
    public void TryMapMdtToSide_2_ReturnsAsk()
    {
        // Act
        var result = _registry.TryMapMdtToSide(2);

        // Assert
        Assert.Equal(Side.Ask, result);
    }

    [Fact]
    public void TryMapMdtToSide_UnknownValue_ReturnsNull()
    {
        // Act
        var result = _registry.TryMapMdtToSide(99);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void TryMapMdtToSide_VariousUnknownValues_ReturnsNull(int mdt)
    {
        // Act
        var result = _registry.TryMapMdtToSide(mdt);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region TryMapOperation String Tests

    [Theory]
    [InlineData("add")]
    [InlineData("Add")]
    [InlineData("ADD")]
    [InlineData("aDd")]
    public void TryMapOperation_String_Add_ReturnsAdd(string value)
    {
        // Act
        var result = _registry.TryMapOperation(value);

        // Assert
        Assert.Equal(Operation.Add, result);
    }

    [Theory]
    [InlineData("update")]
    [InlineData("Update")]
    [InlineData("UPDATE")]
    [InlineData("uPdAtE")]
    public void TryMapOperation_String_Update_ReturnsUpdate(string value)
    {
        // Act
        var result = _registry.TryMapOperation(value);

        // Assert
        Assert.Equal(Operation.Update, result);
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("Delete")]
    [InlineData("DELETE")]
    [InlineData("dElEtE")]
    public void TryMapOperation_String_Delete_ReturnsDelete(string value)
    {
        // Act
        var result = _registry.TryMapOperation(value);

        // Assert
        Assert.Equal(Operation.Delete, result);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("insert")]
    [InlineData("remove")]
    [InlineData("")]
    [InlineData(null)]
    public void TryMapOperation_String_Unknown_ReturnsNull(string? value)
    {
        // Act
        var result = _registry.TryMapOperation(value!);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region TryMapOperation Int Tests

    [Fact]
    public void TryMapOperation_Int_0_ReturnsAdd()
    {
        // Act
        var result = _registry.TryMapOperation(0);

        // Assert
        Assert.Equal(Operation.Add, result);
    }

    [Fact]
    public void TryMapOperation_Int_1_ReturnsUpdate()
    {
        // Act
        var result = _registry.TryMapOperation(1);

        // Assert
        Assert.Equal(Operation.Update, result);
    }

    [Fact]
    public void TryMapOperation_Int_2_ReturnsDelete()
    {
        // Act
        var result = _registry.TryMapOperation(2);

        // Assert
        Assert.Equal(Operation.Delete, result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void TryMapOperation_Int_Unknown_ReturnsNull(int value)
    {
        // Act
        var result = _registry.TryMapOperation(value);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region IsKnownMdt Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void IsKnownMdt_ValidValues_ReturnsTrue(int mdt)
    {
        // Act
        var result = _registry.IsKnownMdt(mdt);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void IsKnownMdt_InvalidValue_ReturnsFalse(int mdt)
    {
        // Act
        var result = _registry.IsKnownMdt(mdt);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsKnownOperation Tests

    [Theory]
    [InlineData("add")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("ADD")]
    [InlineData("Update")]
    public void IsKnownOperation_String_ValidValues_ReturnsTrue(string value)
    {
        // Act
        var result = _registry.IsKnownOperation(value);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void IsKnownOperation_String_InvalidValue_ReturnsFalse(string? value)
    {
        // Act
        var result = _registry.IsKnownOperation(value!);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void IsKnownOperation_Int_ValidValues_ReturnsTrue(int value)
    {
        // Act
        var result = _registry.IsKnownOperation(value);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void IsKnownOperation_Int_InvalidValue_ReturnsFalse(int value)
    {
        // Act
        var result = _registry.IsKnownOperation(value);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Configurability Tests

    [Fact]
    public void MdtToSide_CanBeOverridden()
    {
        // Arrange
        var registry = new MappingRegistry();
        registry.MdtToSide = new Dictionary<int, Side>
        {
            { 66, Side.Bid },  // 'B' ASCII
            { 65, Side.Ask }   // 'A' ASCII
        };

        // Act & Assert
        Assert.Equal(Side.Bid, registry.TryMapMdtToSide(66));
        Assert.Equal(Side.Ask, registry.TryMapMdtToSide(65));
        Assert.Null(registry.TryMapMdtToSide(1)); // Original mapping no longer works
    }

    [Fact]
    public void OperationMapping_CanBeExtended()
    {
        // Arrange
        var registry = new MappingRegistry();
        registry.OperationStringMapping["insert"] = Operation.Add;
        registry.OperationStringMapping["remove"] = Operation.Delete;

        // Act & Assert
        Assert.Equal(Operation.Add, registry.TryMapOperation("insert"));
        Assert.Equal(Operation.Delete, registry.TryMapOperation("remove"));
        // Original mappings still work
        Assert.Equal(Operation.Add, registry.TryMapOperation("add"));
    }

    #endregion
}
