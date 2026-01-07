using Xunit;
using QTS.Backtest.Adapter.Configuration;

namespace QTS.Backtest.Adapter.Tests.Configuration;

public class ConfigValidatorTests
{
    #region Validate - Exception Tests

    [Fact]
    public void Validate_NullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        AdapterConfig? config = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => ConfigValidator.Validate(config!));
        Assert.Contains("null", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_BucketMsZero_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdapterConfig { BucketMs = 0 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ConfigValidator.Validate(config));
        Assert.Contains("BucketMs", ex.Message);
    }

    [Fact]
    public void Validate_BucketMsNegative_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdapterConfig { BucketMs = -10 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ConfigValidator.Validate(config));
        Assert.Contains("BucketMs", ex.Message);
    }

    [Fact]
    public void Validate_StaleThresholdSoftMsZero_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdapterConfig { StaleThresholdSoftMs = 0 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ConfigValidator.Validate(config));
        Assert.Contains("StaleThresholdSoftMs", ex.Message);
    }

    [Fact]
    public void Validate_StaleThresholdHardLessThanSoft_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdapterConfig
        {
            StaleThresholdSoftMs = 1000,
            StaleThresholdHardMs = 500 // Less than soft
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ConfigValidator.Validate(config));
        Assert.Contains("StaleThresholdHardMs", ex.Message);
        Assert.Contains("StaleThresholdSoftMs", ex.Message);
    }

    [Fact]
    public void Validate_LevelAcceptNull_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdapterConfig { LevelAccept = null! };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ConfigValidator.Validate(config));
        Assert.Contains("LevelAccept", ex.Message);
    }

    [Fact]
    public void Validate_LevelAcceptEmpty_ThrowsArgumentException()
    {
        // Arrange
        var config = new AdapterConfig { LevelAccept = new HashSet<int>() };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ConfigValidator.Validate(config));
        Assert.Contains("LevelAccept", ex.Message);
    }

    [Fact]
    public void Validate_DefaultConfig_DoesNotThrow()
    {
        // Arrange
        var config = AdapterConfig.Default;

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => ConfigValidator.Validate(config));
        Assert.Null(exception);
    }

    #endregion

    #region TryValidate Tests

    [Fact]
    public void TryValidate_InvalidConfig_ReturnsFalseWithError()
    {
        // Arrange
        var config = new AdapterConfig { BucketMs = 0 };

        // Act
        var result = ConfigValidator.TryValidate(config, out var error);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("BucketMs", error);
    }

    [Fact]
    public void TryValidate_ValidConfig_ReturnsTrueNoError()
    {
        // Arrange
        var config = AdapterConfig.Default;

        // Act
        var result = ConfigValidator.TryValidate(config, out var error);

        // Assert
        Assert.True(result);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_NullConfig_ReturnsFalseWithError()
    {
        // Arrange
        AdapterConfig? config = null;

        // Act
        var result = ConfigValidator.TryValidate(config!, out var error);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("null", error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_StaleThresholdHardEqualsSoft_DoesNotThrow()
    {
        // Arrange - Equal values should be valid
        var config = new AdapterConfig
        {
            StaleThresholdSoftMs = 500,
            StaleThresholdHardMs = 500
        };

        // Act & Assert
        var exception = Record.Exception(() => ConfigValidator.Validate(config));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_MultipleLevels_DoesNotThrow()
    {
        // Arrange
        var config = new AdapterConfig
        {
            LevelAccept = new HashSet<int> { 1, 2, 3 }
        };

        // Act & Assert
        var exception = Record.Exception(() => ConfigValidator.Validate(config));
        Assert.Null(exception);
    }

    #endregion
}
