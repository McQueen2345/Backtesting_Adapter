using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Models;
using QTS.Edge.Core.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Stability;

/// <summary>
/// Phase C: Numerische Stabilität Tests.
/// Stellt sicher, dass keine Division by Zero, Overflow, NaN auftreten.
/// </summary>
public class NumericalStabilityTests
{
    private readonly ITestOutputHelper _output;
    private readonly EdgeConfiguration _config;

    public NumericalStabilityTests(ITestOutputHelper output)
    {
        _output = output;
        _config = EdgeConfiguration.Default;
    }

    [Fact]
    public void ZScore_MadZero_UsesEpsilonNotDivideByZero()
    {
        var zCalc = new ZScoreCalculator(_config);

        // Wenn MAD = 0 (alle Werte gleich)
        // → Epsilon wird verwendet, kein NaN/Infinity
        var zScore = zCalc.Calculate(0.5, 0.5, 0.0);

        double.IsNaN(zScore).Should().BeFalse("MAD=0 should not produce NaN");
        double.IsInfinity(zScore).Should().BeFalse("MAD=0 should not produce Infinity");

        // Bei MAD=0 und value=median sollte Z=0 sein
        zScore.Should().Be(0.0, "When value equals median and MAD=0, Z should be 0");

        _output.WriteLine($"Z-Score with MAD=0: {zScore}");
    }

    [Fact]
    public void ZScore_VerySmallMad_NoOverflow()
    {
        var zCalc = new ZScoreCalculator(_config);

        // MAD = sehr klein (aber > Epsilon)
        double mad = 1e-9;
        var zScore = zCalc.Calculate(0.5, 0.0, mad);

        double.IsNaN(zScore).Should().BeFalse("Very small MAD should not produce NaN");
        double.IsInfinity(zScore).Should().BeFalse("Very small MAD should not produce Infinity");

        // Sollte geclippt sein
        Math.Abs(zScore).Should().BeLessThanOrEqualTo(5.0, "Z should be clipped to ±5");

        _output.WriteLine($"Z-Score with tiny MAD ({mad}): {zScore}");
    }

    [Fact]
    public void ZScore_VeryLargeValue_ClipsCorrectly()
    {
        var zCalc = new ZScoreCalculator(_config);

        // Value = 1000, Median = 0, MAD = 0.001
        // Z wäre riesig, wird auf ±5 geclippt
        var zScore = zCalc.Calculate(1000.0, 0.0, 0.001);

        zScore.Should().Be(5.0, "Extreme Z should be clipped to +5");

        var zScoreNeg = zCalc.Calculate(-1000.0, 0.0, 0.001);
        zScoreNeg.Should().Be(-5.0, "Extreme negative Z should be clipped to -5");

        _output.WriteLine($"Large value Z: {zScore}");
        _output.WriteLine($"Large negative value Z: {zScoreNeg}");
    }

    [Fact]
    public void StructImb_LargeVolumes_NoIntegerOverflow()
    {
        var calc = new StructImbCalculator();

        // BidSize = 1_000_000, AskSize = 1_000_000
        var result = calc.Calculate(1_000_000, 1_000_000);

        double.IsNaN(result).Should().BeFalse("Large volumes should not produce NaN");
        double.IsInfinity(result).Should().BeFalse("Large volumes should not produce Infinity");
        result.Should().BeApproximately(0.0, 0.0001, "Equal large volumes should give 0");

        // Asymmetrische große Werte
        var result2 = calc.Calculate(1_000_000, 500_000);
        result2.Should().BeInRange(-1.0, 1.0, "Result should be in valid range");

        _output.WriteLine($"StructImb(1M, 1M): {result}");
        _output.WriteLine($"StructImb(1M, 500K): {result2}");
    }

    [Fact]
    public void RollingStats_ExtremeValues_StableMedian()
    {
        var stats = new RollingStatistics(100, 10);

        // Mix aus sehr großen und sehr kleinen Werten
        for (int i = 0; i < 50; i++)
        {
            stats.Add(i % 2 == 0 ? 1e10 : 1e-10);
        }

        var median = stats.GetMedian();
        var mad = stats.GetMad();

        double.IsNaN(median).Should().BeFalse("Extreme values should not produce NaN median");
        double.IsInfinity(median).Should().BeFalse("Extreme values should not produce Infinity median");
        double.IsNaN(mad).Should().BeFalse("Extreme values should not produce NaN MAD");
        double.IsInfinity(mad).Should().BeFalse("Extreme values should not produce Infinity MAD");

        _output.WriteLine($"Median with extreme values: {median:E5}");
        _output.WriteLine($"MAD with extreme values: {mad:E5}");
    }

    [Fact]
    public void RollingStats_NaN_Behavior()
    {
        var stats = new RollingStatistics(100, 10);

        // Füge normale Werte hinzu
        for (int i = 0; i < 20; i++)
        {
            stats.Add(i * 0.1);
        }

        var medianBefore = stats.GetMedian();

        // Median sollte normal berechnet worden sein
        double.IsNaN(medianBefore).Should().BeFalse();

        _output.WriteLine($"Median with normal values: {medianBefore}");
    }

    [Fact]
    public void ZScore_ResultNeverNaN()
    {
        var zCalc = new ZScoreCalculator(_config);

        var testCases = new[]
        {
            (0.0, 0.0, 0.0),
            (1.0, 0.0, 0.0),
            (0.0, 1.0, 0.0),
            (1e10, 0.0, 1e-10),
            (-1e10, 0.0, 1e-10),
            (double.Epsilon, 0.0, double.Epsilon),
        };

        foreach (var (value, median, mad) in testCases)
        {
            var z = zCalc.Calculate(value, median, mad);
            double.IsNaN(z).Should().BeFalse($"Z({value}, {median}, {mad}) should not be NaN");
        }

        _output.WriteLine("All Z-Score calculations avoid NaN");
    }

    [Fact]
    public void ZScore_ResultNeverInfinity()
    {
        var zCalc = new ZScoreCalculator(_config);

        var testCases = new[]
        {
            (1e100, 0.0, 1e-100),
            (-1e100, 0.0, 1e-100),
            (1e308, 0.0, 1e-308),
        };

        foreach (var (value, median, mad) in testCases)
        {
            var z = zCalc.Calculate(value, median, mad);
            double.IsInfinity(z).Should().BeFalse($"Z({value:E}, {median}, {mad:E}) should not be Infinity");
            Math.Abs(z).Should().BeLessThanOrEqualTo(5.0, "Z should be clipped");
        }

        _output.WriteLine("All Z-Score calculations avoid Infinity");
    }

    [Fact]
    public void SpreadGate_DecimalPrecision_NoFloatErrors()
    {
        var gate = new SpreadQualityGate(_config);

        // KRITISCH: Spread-Berechnung muss in decimal sein!
        // Test: AskPrice=5001.00, BidPrice=5000.00, TickSize=0.25
        // → spreadTicks = 4 exakt (nicht 3.9999999 oder 4.0000001)
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5001.00m,  // Genau 4 Ticks
            BidSize: 100,
            AskSize: 100
        );

        gate.Check(snapshot).Should().BeTrue("4 ticks spread should pass with exact decimal math");

        // Ein Tick mehr sollte fehlschlagen
        var snapshotOver = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5001.25m,  // Genau 5 Ticks
            BidSize: 100,
            AskSize: 100
        );

        gate.Check(snapshotOver).Should().BeFalse("5 ticks spread should fail");

        _output.WriteLine("Decimal precision for spread calculation: CORRECT");
    }

    [Fact]
    public void SpreadGate_VariousPrices_NoFloatIssues()
    {
        var gate = new SpreadQualityGate(_config);

        // Teste verschiedene Preislevels
        var prices = new[] { 1000.00m, 5000.00m, 10000.00m, 50000.00m };

        foreach (var bidPrice in prices)
        {
            var snapshot = new DomSnapshot(
                Timestamp: DateTime.UtcNow,
                BidPrice: bidPrice,
                AskPrice: bidPrice + 1.00m,  // 4 Ticks
                BidSize: 100,
                AskSize: 100
            );

            gate.Check(snapshot).Should().BeTrue($"4 ticks at price {bidPrice} should pass");
        }

        _output.WriteLine("Spread gate works at all price levels");
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(1e-10, 1e-10, 1e-10)]
    [InlineData(1e10, 1e10, 1e10)]
    [InlineData(-1e10, 1e10, 1e5)]
    [InlineData(0.5, 0.5, 0.0)]
    [InlineData(0.999, 0.0, 0.001)]
    public void ZScore_ExtremeInputs_NeverCrashes(double value, double median, double mad)
    {
        var zCalc = new ZScoreCalculator(_config);

        // Sollte niemals crashen
        var action = () => zCalc.Calculate(value, median, mad);
        action.Should().NotThrow();

        var z = zCalc.Calculate(value, median, mad);
        double.IsNaN(z).Should().BeFalse();
        double.IsInfinity(z).Should().BeFalse();
        Math.Abs(z).Should().BeLessThanOrEqualTo(5.0);

        _output.WriteLine($"Z({value:E2}, {median:E2}, {mad:E2}) = {z}");
    }

    [Fact]
    public void FullPipeline_NeverProducesNaN()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;
        var random = new Random(42);

        for (int i = 0; i < 1000; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: random.Next(1, 10000),
                AskSize: random.Next(1, 10000)
            );

            var result = edge.ProcessSnapshot(snapshot);

            double.IsNaN(result.StructImbZ).Should().BeFalse($"Z at {i} should not be NaN");
            double.IsInfinity(result.StructImbZ).Should().BeFalse($"Z at {i} should not be Infinity");
        }

        _output.WriteLine("1000 random snapshots: No NaN or Infinity produced");
    }

    [Fact]
    public void EdgeConfiguration_Epsilon_IsUsed()
    {
        // Epsilon sollte 1e-10 sein
        _config.Epsilon.Should().Be(1e-10, "Epsilon should be 1e-10");

        var zCalc = new ZScoreCalculator(_config);

        // Bei MAD knapp unter Epsilon sollte Z=0 sein
        var zScore = zCalc.Calculate(0.5, 0.0, 0.5e-10);
        zScore.Should().Be(0.0, "When MAD < Epsilon, Z should be 0");

        _output.WriteLine($"Epsilon: {_config.Epsilon}");
        _output.WriteLine($"Z with MAD < Epsilon: {zScore}");
    }
}
