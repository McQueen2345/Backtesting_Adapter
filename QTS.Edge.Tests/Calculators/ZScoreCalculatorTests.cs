using FluentAssertions;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using Xunit;

namespace QTS.Edge.Tests.Calculators;

public class ZScoreCalculatorTests
{
    private readonly ZScoreCalculator _calculator;

    public ZScoreCalculatorTests()
    {
        _calculator = new ZScoreCalculator(EdgeConfiguration.Default);
    }

    [Fact]
    public void Calculate_NormalValues_ReturnsCorrectZ()
    {
        // value=5, median=3, mad=1
        // Z = (5 - 3) / (1.4826 * 1) = 2 / 1.4826 ≈ 1.349
        var result = _calculator.Calculate(5.0, 3.0, 1.0);
        result.Should().BeApproximately(1.349, 0.001);
    }

    [Fact]
    public void Calculate_MadBelowEpsilon_ReturnsZero()
    {
        // MAD = 0.0001 < Epsilon (0.001) → return 0.0
        var result = _calculator.Calculate(10.0, 5.0, 0.0001);
        result.Should().Be(0.0);
    }

    [Fact]
    public void Calculate_ExceedsClip_ReturnsClippedValue()
    {
        // Sehr großer Z-Wert → sollte auf +5.0 geclippt werden
        // value=100, median=0, mad=1
        // Z = (100 - 0) / (1.4826 * 1) = 67.45 → clipped to +5.0
        var result = _calculator.Calculate(100.0, 0.0, 1.0);
        result.Should().Be(5.0);
    }

    [Fact]
    public void Calculate_NegativeZ_ClipsCorrectly()
    {
        // Sehr negativer Z-Wert → sollte auf -5.0 geclippt werden
        // value=-100, median=0, mad=1
        // Z = (-100 - 0) / (1.4826 * 1) = -67.45 → clipped to -5.0
        var result = _calculator.Calculate(-100.0, 0.0, 1.0);
        result.Should().Be(-5.0);
    }

    [Fact]
    public void Calculate_AtClipBoundary_ReturnsExactClip()
    {
        // Z genau an der Grenze → sollte exakt ±5.0 sein
        // Wir berechnen rückwärts: Z=5.0 → value = median + 5.0 * 1.4826 * mad
        // Bei median=0, mad=1: value = 5.0 * 1.4826 = 7.413
        var result = _calculator.Calculate(7.413, 0.0, 1.0);
        result.Should().BeApproximately(5.0, 0.001);
    }
}
