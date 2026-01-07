using FluentAssertions;
using QTS.Edge.Core.Calculators;
using Xunit;

namespace QTS.Edge.Tests.Calculators;

public class StructImbCalculatorTests
{
    private readonly StructImbCalculator _calculator = new();

    [Fact]
    public void Calculate_EqualSizes_ReturnsZero()
    {
        var result = _calculator.Calculate(100, 100);
        result.Should().Be(0.0);
    }

    [Fact]
    public void Calculate_BidOnly_ReturnsPositiveOne()
    {
        var result = _calculator.Calculate(100, 0);
        result.Should().Be(1.0);
    }

    [Fact]
    public void Calculate_AskOnly_ReturnsNegativeOne()
    {
        var result = _calculator.Calculate(0, 100);
        result.Should().Be(-1.0);
    }

    [Fact]
    public void Calculate_BothZero_ReturnsZero()
    {
        var result = _calculator.Calculate(0, 0);
        result.Should().Be(0.0);
    }

    [Fact]
    public void Calculate_NegativeValues_TreatedAsZero()
    {
        var result = _calculator.Calculate(-50, 100);
        result.Should().Be(-1.0);
    }

    [Fact]
    public void Calculate_TypicalImbalance_ReturnsCorrectValue()
    {
        // Bid=150, Ask=50 → (150-50)/(150+50) = 100/200 = 0.5
        var result = _calculator.Calculate(150, 50);
        result.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public void Calculate_ResultAlwaysBetweenMinusOneAndPlusOne()
    {
        var result1 = _calculator.Calculate(1000, 1);
        var result2 = _calculator.Calculate(1, 1000);

        result1.Should().BeInRange(-1.0, 1.0);
        result2.Should().BeInRange(-1.0, 1.0);
    }

    // === T032: Edge Cases ===

    [Fact]
    public void Calculate_BothNegative_ReturnsZero()
    {
        var result = _calculator.Calculate(-100, -50);
        result.Should().Be(0.0);
    }

    [Fact]
    public void Calculate_LargeValues_HandlesCorrectly()
    {
        // Große Werte sollten korrekt berechnet werden
        var result = _calculator.Calculate(1_000_000, 500_000);
        // (1000000 - 500000) / (1000000 + 500000) = 500000 / 1500000 = 0.333...
        result.Should().BeApproximately(0.3333, 0.001);
    }

    [Fact]
    public void Calculate_VerySmallDifference_ReturnsSmallValue()
    {
        var result = _calculator.Calculate(101, 100);
        // (101 - 100) / (101 + 100) = 1 / 201 ≈ 0.00497
        result.Should().BeApproximately(0.00497, 0.001);
    }

    [Fact]
    public void Calculate_OneNegativeOnPositive_TreatsNegativeAsZero()
    {
        // Bid=-100 → 0, Ask=100 → (0-100)/(0+100) = -1.0
        var result = _calculator.Calculate(-100, 100);
        result.Should().Be(-1.0);
    }

    // === T033: Boundary Tests ===

    [Theory]
    [InlineData(100, 0, 1.0)]      // Max positiv
    [InlineData(0, 100, -1.0)]     // Max negativ
    [InlineData(50, 50, 0.0)]      // Exakt neutral
    [InlineData(75, 25, 0.5)]      // Positiv
    [InlineData(25, 75, -0.5)]     // Negativ
    public void Calculate_VariousInputs_ReturnsExpectedValue(int bid, int ask, double expected)
    {
        var result = _calculator.Calculate(bid, ask);
        result.Should().BeApproximately(expected, 0.0001);
    }

    [Fact]
    public void Calculate_MaxInt_HandlesWithoutOverflow()
    {
        // Sollte nicht überlaufen bei großen int-Werten
        var result = _calculator.Calculate(int.MaxValue / 2, int.MaxValue / 2);
        result.Should().Be(0.0);
    }

    [Fact]
    public void Calculate_AsymmetricBoundary_CorrectSign()
    {
        // Bid > Ask → positiv
        _calculator.Calculate(51, 49).Should().BePositive();
        // Ask > Bid → negativ
        _calculator.Calculate(49, 51).Should().BeNegative();
    }
}
