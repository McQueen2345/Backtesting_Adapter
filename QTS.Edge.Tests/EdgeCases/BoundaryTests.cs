using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;
using QTS.Edge.Core.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.EdgeCases;

/// <summary>
/// Phase B: Edge Cases & Grenzwerte Tests.
/// Testet Verhalten an exakten Grenzen und bei Extremwerten.
/// </summary>
public class BoundaryTests
{
    private readonly ITestOutputHelper _output;
    private readonly EdgeConfiguration _config;

    public BoundaryTests(ITestOutputHelper output)
    {
        _output = output;
        _config = EdgeConfiguration.Default;
    }

    // ============================================================
    // STRUCTIMB CALCULATOR BOUNDARIES
    // ============================================================

    [Fact]
    public void StructImb_MaxInt_HandlesCorrectly()
    {
        var calc = new StructImbCalculator();

        // int.MaxValue für BidSize
        var result1 = calc.Calculate(int.MaxValue, 1);
        var result2 = calc.Calculate(1, int.MaxValue);
        var result3 = calc.Calculate(int.MaxValue, int.MaxValue);

        // Darf nicht überlaufen!
        double.IsNaN(result1).Should().BeFalse("MaxInt should not produce NaN");
        double.IsNaN(result2).Should().BeFalse("MaxInt should not produce NaN");
        double.IsNaN(result3).Should().BeFalse("MaxInt should not produce NaN");

        double.IsInfinity(result1).Should().BeFalse("MaxInt should not produce Infinity");
        double.IsInfinity(result2).Should().BeFalse("MaxInt should not produce Infinity");
        double.IsInfinity(result3).Should().BeFalse("MaxInt should not produce Infinity");

        result3.Should().BeApproximately(0.0, 0.001, "Equal MaxInt values should give ~0");

        _output.WriteLine($"MaxInt/1: {result1}");
        _output.WriteLine($"1/MaxInt: {result2}");
        _output.WriteLine($"MaxInt/MaxInt: {result3}");
    }

    [Fact]
    public void StructImb_ZeroZero_ReturnsZero()
    {
        var calc = new StructImbCalculator();

        // Bid=0, Ask=0 → 0.0 (nicht NaN, nicht Exception)
        var result = calc.Calculate(0, 0);

        result.Should().Be(0.0, "Zero/Zero should return 0, not NaN");
        double.IsNaN(result).Should().BeFalse();

        _output.WriteLine($"0/0: {result}");
    }

    [Fact]
    public void StructImb_NegativeValues_TreatedAsZero()
    {
        var calc = new StructImbCalculator();

        // Negative Inputs → als 0 behandeln
        var result1 = calc.Calculate(-100, 100);
        var result2 = calc.Calculate(100, -100);
        var result3 = calc.Calculate(-100, -100);

        // Sollte nicht crashen
        double.IsNaN(result1).Should().BeFalse();
        double.IsNaN(result2).Should().BeFalse();
        double.IsNaN(result3).Should().BeFalse();

        _output.WriteLine($"-100/100: {result1}");
        _output.WriteLine($"100/-100: {result2}");
        _output.WriteLine($"-100/-100: {result3}");
    }

    [Fact]
    public void StructImb_ExtremeImbalance_StaysInRange()
    {
        var calc = new StructImbCalculator();

        // Ergebnis muss IMMER in [-1, +1] sein
        var testCases = new[]
        {
            (1000000, 1),
            (1, 1000000),
            (int.MaxValue, 1),
            (1, int.MaxValue),
            (int.MaxValue / 2, 1),
        };

        foreach (var (bid, ask) in testCases)
        {
            var result = calc.Calculate(bid, ask);
            result.Should().BeInRange(-1.0, 1.0, $"StructImb({bid}, {ask}) should be in [-1, 1]");
        }

        _output.WriteLine("All extreme imbalance values stay in [-1, 1]");
    }

    // ============================================================
    // ROLLING STATISTICS BOUNDARIES
    // ============================================================

    [Fact]
    public void RollingStats_ExactlyMinWarmup_IsWarm()
    {
        var stats = new RollingStatistics(_config.WindowSize, _config.MinWarmupSamples);

        // 199 Samples → IsWarm = false
        for (int i = 0; i < 199; i++)
        {
            stats.Add(0.1);
        }
        stats.IsWarm.Should().BeFalse("199 samples should not be warm");

        // 200 Samples → IsWarm = true
        stats.Add(0.1);
        stats.IsWarm.Should().BeTrue("200 samples should be warm");

        _output.WriteLine($"MinWarmupSamples: {_config.MinWarmupSamples}");
        _output.WriteLine("Boundary at MinWarmup works correctly");
    }

    [Fact]
    public void RollingStats_ExactlyWindowSize_NoOverflow()
    {
        // Verwende kleineres Window für Test-Performance
        var stats = new RollingStatistics(250, 10);

        // Genau WindowSize Samples
        for (int i = 0; i < 250; i++)
        {
            stats.Add(i * 0.01);
        }

        stats.Count.Should().Be(250, "Count should equal WindowSize");

        _output.WriteLine($"Count at WindowSize: {stats.Count}");
    }

    [Fact]
    public void RollingStats_WindowOverflow_FIFO()
    {
        // Verwende kleineres Window für Test
        var stats = new RollingStatistics(100, 10);

        // Mehr als WindowSize Samples
        for (int i = 0; i < 150; i++)
        {
            stats.Add(i);
        }

        stats.Count.Should().Be(100, "Count should be capped at WindowSize");

        // Die ältesten sollten entfernt worden sein
        // Bei FIFO: 0-49 entfernt, 50-149 im Buffer
        var median = stats.GetMedian();
        median.Should().BeGreaterThan(50, "Oldest values should be removed (FIFO)");

        _output.WriteLine($"After overflow - Count: {stats.Count}, Median: {median}");
    }

    [Fact]
    public void RollingStats_SingleValue_MedianIsThatValue()
    {
        var stats = new RollingStatistics(100, 1);

        stats.Add(42.0);

        var median = stats.GetMedian();
        median.Should().Be(42.0, "Single value median should be that value");

        _output.WriteLine($"Single value median: {median}");
    }

    [Fact]
    public void RollingStats_AllSameValues_MadIsZero()
    {
        var stats = new RollingStatistics(100, 10);

        // Alle gleich
        for (int i = 0; i < 50; i++)
        {
            stats.Add(0.5);
        }

        var mad = stats.GetMad();
        mad.Should().Be(0.0, "MAD of constant values should be 0");

        _output.WriteLine($"MAD of constant values: {mad}");
    }

    // ============================================================
    // Z-SCORE CALCULATOR BOUNDARIES
    // ============================================================

    [Fact]
    public void ZScore_ExactlyAtThreshold_GeneratesSignal()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // Z = 1.5 exakt → Signal sollte generiert werden
        var signal = gen.Generate(1.5, baseTime, true, false, true);

        signal.Should().Be(1, "Z = 1.5 exactly should generate LONG signal");

        _output.WriteLine($"Signal at Z=1.5: {signal}");
    }

    [Fact]
    public void ZScore_JustBelowThreshold_NoSignal()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // Z = 1.4999 → Kein Signal
        var signal = gen.Generate(1.4999, baseTime, true, false, true);

        signal.Should().Be(0, "Z = 1.4999 should not generate signal");

        _output.WriteLine($"Signal at Z=1.4999: {signal}");
    }

    [Fact]
    public void ZScore_AtClipBoundary_IsClipped()
    {
        var zCalc = new ZScoreCalculator(_config);

        // Z würde sehr groß sein
        var zHigh = zCalc.Calculate(1.0, 0.0, 0.001);  // (1.0 - 0) / (0.001 * 1.4826) = huge
        var zLow = zCalc.Calculate(-1.0, 0.0, 0.001);

        zHigh.Should().Be(5.0, "Extreme high Z should be clipped to +5");
        zLow.Should().Be(-5.0, "Extreme low Z should be clipped to -5");

        _output.WriteLine($"Clipped Z high: {zHigh}");
        _output.WriteLine($"Clipped Z low: {zLow}");
    }

    // ============================================================
    // SIGNAL GENERATOR BOUNDARIES
    // ============================================================

    [Fact]
    public void Signal_ExactlyAtHysteresisThreshold_Changes()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // Erst LONG
        gen.Generate(1.8, baseTime, true, false, true);
        gen.CurrentSignal.Should().Be(1);

        // LONG→SHORT braucht Z ≤ -2.25 (Entry * (1 + Hysteresis) = 1.5 * 1.5)
        // Z = -2.25 exakt → Wechsel erlaubt (wenn Cooldown OK)
        var signal = gen.Generate(-2.25, baseTime.AddSeconds(2), true, false, true);

        signal.Should().Be(-1, "Z = -2.25 exactly should trigger reversal to SHORT");

        _output.WriteLine($"Signal at hysteresis threshold: {signal}");
    }

    [Fact]
    public void Signal_JustAboveHysteresisThreshold_NoChange()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // Erst LONG
        gen.Generate(1.8, baseTime, true, false, true);
        gen.CurrentSignal.Should().Be(1);

        // Z = -2.24 → bleibt LONG (kein Wechsel zu SHORT)
        var signal = gen.Generate(-2.24, baseTime.AddSeconds(2), true, false, true);

        // Sollte entweder LONG bleiben oder zu FLAT wechseln (nicht zu SHORT)
        signal.Should().NotBe(-1, "Z = -2.24 should not trigger reversal to SHORT");

        _output.WriteLine($"Signal just above hysteresis: {signal}");
    }

    [Fact]
    public void Signal_ExactlyAtExitThreshold_Exits()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // Erst LONG
        gen.Generate(1.8, baseTime, true, false, true);
        gen.CurrentSignal.Should().Be(1);

        // LONG exit bei Z < 0.75 (Exit Threshold = Entry * HysteresisFactor = 1.5 * 0.5)
        // Z = 0.74 → wird FLAT
        var signal = gen.Generate(0.74, baseTime.AddSeconds(2), true, false, true);

        signal.Should().Be(0, "Z = 0.74 should exit to FLAT");

        _output.WriteLine($"Signal at exit threshold: {signal}");
    }

    [Fact]
    public void Signal_ExactlyAtCooldown_CanChange()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // LONG at T=0
        gen.Generate(1.8, baseTime, true, false, true);

        // Nach exakt 1001ms → Wechsel erlaubt
        var signal = gen.Generate(-2.5, baseTime.AddMilliseconds(1001), true, false, true);

        signal.Should().Be(-1, "After 1001ms cooldown, reversal should be allowed");

        _output.WriteLine($"Signal after cooldown: {signal}");
    }

    [Fact]
    public void Signal_JustBeforeCooldown_CannotChange()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // LONG at T=0
        gen.Generate(1.8, baseTime, true, false, true);

        // Nach 999ms → Wechsel noch blockiert
        var signal = gen.Generate(-2.5, baseTime.AddMilliseconds(999), true, false, true);

        signal.Should().Be(1, "Before cooldown expires, reversal should be blocked");

        _output.WriteLine($"Signal before cooldown: {signal}");
    }

    // ============================================================
    // QUALITY GATE BOUNDARIES
    // ============================================================

    [Fact]
    public void SpreadGate_ExactlyAtMax_Passes()
    {
        var gate = new SpreadQualityGate(_config);

        // Spread = 4 Ticks exakt (4 * 0.25 = 1.00)
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5001.00m,
            BidSize: 100,
            AskSize: 100
        );

        gate.Check(snapshot).Should().BeTrue("4 ticks spread should pass");

        _output.WriteLine("SpreadGate at exactly MaxSpreadTicks: PASS");
    }

    [Fact]
    public void SpreadGate_JustOverMax_Fails()
    {
        var gate = new SpreadQualityGate(_config);

        // Spread = 5 Ticks (5 * 0.25 = 1.25)
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5001.25m,
            BidSize: 100,
            AskSize: 100
        );

        gate.Check(snapshot).Should().BeFalse("5 ticks spread should fail");

        _output.WriteLine("SpreadGate over MaxSpreadTicks: FAIL");
    }

    [Fact]
    public void DepthGate_ExactlyAtMin_Passes()
    {
        var gate = new DepthQualityGate(_config);

        // BidSize=1, AskSize=1 → Pass (MinDepthL1 = 1)
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 1,
            AskSize: 1
        );

        gate.Check(snapshot).Should().BeTrue("Minimum depth should pass");

        _output.WriteLine("DepthGate at exactly MinDepthL1: PASS");
    }

    [Fact]
    public void DepthGate_ZeroOnOneSide_Fails()
    {
        var gate = new DepthQualityGate(_config);

        var snapshot1 = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 0,
            AskSize: 100
        );

        var snapshot2 = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 0
        );

        gate.Check(snapshot1).Should().BeFalse("BidSize=0 should fail");
        gate.Check(snapshot2).Should().BeFalse("AskSize=0 should fail");

        _output.WriteLine("DepthGate with zero depth: FAIL");
    }

    [Fact]
    public void ZScore_NegativeThreshold_ShortSignal()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // Z = -1.5 exakt → SHORT Signal
        var signal = gen.Generate(-1.5, baseTime, true, false, true);

        signal.Should().Be(-1, "Z = -1.5 exactly should generate SHORT signal");

        _output.WriteLine($"Signal at Z=-1.5: {signal}");
    }

    [Fact]
    public void ZScore_JustAboveNegativeThreshold_NoSignal()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // Z = -1.4999 → Kein Signal
        var signal = gen.Generate(-1.4999, baseTime, true, false, true);

        signal.Should().Be(0, "Z = -1.4999 should not generate SHORT signal");

        _output.WriteLine($"Signal at Z=-1.4999: {signal}");
    }
}
