using Xunit;
using QTS.Backtest.Adapter.Configuration;
using QTS.Backtest.Contracts.Enums;

namespace QTS.Backtest.Adapter.Tests.Configuration;

public class AdapterConfigTests
{
    [Fact]
    public void Default_EmitPolicy_IsOnBboChange()
    {
        // Arrange & Act
        var config = AdapterConfig.Default;

        // Assert
        Assert.Equal(EmitPolicy.OnBboChange, config.EmitPolicy);
    }

    [Fact]
    public void Default_BucketMs_Is100()
    {
        // Arrange & Act
        var config = AdapterConfig.Default;

        // Assert
        Assert.Equal(100, config.BucketMs);
    }

    [Fact]
    public void Default_LevelAccept_ContainsOnly2()
    {
        // Arrange & Act
        var config = AdapterConfig.Default;

        // Assert
        Assert.Single(config.LevelAccept);
        Assert.Contains(2, config.LevelAccept);
    }

    [Fact]
    public void Default_MissingLevelPolicy_IsAccept()
    {
        // Arrange & Act
        var config = AdapterConfig.Default;

        // Assert
        Assert.Equal(MissingLevelPolicy.Accept, config.MissingLevelPolicy);
    }

    [Fact]
    public void Default_EnableDeduplication_IsFalse()
    {
        // Arrange & Act
        var config = AdapterConfig.Default;

        // Assert
        Assert.False(config.EnableDeduplication);
    }

    [Fact]
    public void Default_StaleThresholdSoftMs_Is500()
    {
        // Arrange & Act
        var config = AdapterConfig.Default;

        // Assert
        Assert.Equal(500, config.StaleThresholdSoftMs);
    }

    [Fact]
    public void Default_StaleThresholdHardMs_Is5000()
    {
        // Arrange & Act
        var config = AdapterConfig.Default;

        // Assert
        Assert.Equal(5000, config.StaleThresholdHardMs);
    }

    [Fact]
    public void DefaultFactory_ReturnsNewInstance()
    {
        // Arrange & Act
        var config1 = AdapterConfig.Default;
        var config2 = AdapterConfig.Default;

        // Assert - Should be different instances
        Assert.NotSame(config1, config2);
    }

    [Fact]
    public void LevelAccept_CanBeModified()
    {
        // Arrange
        var config = AdapterConfig.Default;

        // Act
        config.LevelAccept = new HashSet<int> { 1, 2, 3 };

        // Assert
        Assert.Equal(3, config.LevelAccept.Count);
        Assert.Contains(1, config.LevelAccept);
        Assert.Contains(2, config.LevelAccept);
        Assert.Contains(3, config.LevelAccept);
    }
}
