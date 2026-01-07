using FluentAssertions;
using QTS.Edge.Core.Configuration;
using Xunit;

namespace QTS.Edge.Tests.Configuration;

public class EdgeConfigurationTests
{
    [Fact]
    public void Default_HasCorrectSnapshotInterval()
    {
        var config = EdgeConfiguration.Default;
        config.SnapshotIntervalMs.Should().Be(100);
    }

    [Fact]
    public void Default_HasCorrectWindowSize()
    {
        var config = EdgeConfiguration.Default;
        config.WindowSize.Should().Be(36_000);
    }

    [Fact]
    public void Default_HasCorrectMinWarmupSamples()
    {
        var config = EdgeConfiguration.Default;
        config.MinWarmupSamples.Should().Be(200);
    }

    [Fact]
    public void Default_HasCorrectZThreshold()
    {
        var config = EdgeConfiguration.Default;
        config.ZThreshold.Should().Be(1.5);
    }

    [Fact]
    public void Default_HasCorrectZClip()
    {
        var config = EdgeConfiguration.Default;
        config.ZClip.Should().Be(5.0);
    }

    [Fact]
    public void Default_HasCorrectMadScale()
    {
        var config = EdgeConfiguration.Default;
        config.MadScale.Should().Be(1.4826);
    }

    [Fact]
    public void Default_HasCorrectEpsilon()
    {
        var config = EdgeConfiguration.Default;
        config.Epsilon.Should().Be(0.001);
    }

    [Fact]
    public void Default_HasCorrectSignalCooldownMs()
    {
        var config = EdgeConfiguration.Default;
        config.SignalCooldownMs.Should().Be(1000);
    }

    [Fact]
    public void Default_HasCorrectHysteresisFactor()
    {
        var config = EdgeConfiguration.Default;
        config.HysteresisFactor.Should().Be(0.5);
    }

    [Fact]
    public void Default_HasQualityGatesEnabled()
    {
        var config = EdgeConfiguration.Default;
        config.EnableQualityGates.Should().BeTrue();
    }

    [Fact]
    public void Default_HasCorrectMaxSpreadTicks()
    {
        var config = EdgeConfiguration.Default;
        config.MaxSpreadTicks.Should().Be(4);
    }

    [Fact]
    public void Default_HasCorrectMinDepthL1()
    {
        var config = EdgeConfiguration.Default;
        config.MinDepthL1.Should().Be(1);
    }

    [Fact]
    public void Default_HasCorrectTickSize()
    {
        var config = EdgeConfiguration.Default;
        config.TickSize.Should().Be(0.25m);
    }

    [Fact]
    public void Default_HasCorrectTickValue()
    {
        var config = EdgeConfiguration.Default;
        config.TickValue.Should().Be(12.50m);
    }
}
