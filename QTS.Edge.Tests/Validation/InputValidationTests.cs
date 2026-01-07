using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Validation;

/// <summary>
/// Phase I: Input-Validation & Security Tests.
/// Testet die Behandlung von ungültigen Eingaben.
/// </summary>
public class InputValidationTests
{
    private readonly ITestOutputHelper _output;
    private readonly EdgeConfiguration _config;

    public InputValidationTests(ITestOutputHelper output)
    {
        _output = output;
        _config = EdgeConfiguration.Default;
    }

    [Fact]
    public void DomSnapshot_NegativeBidSize_HandledGracefully()
    {
        var calc = new StructImbCalculator();

        // BidSize = -100 → wird als 0 behandelt oder sinnvoll verarbeitet
        var result = calc.Calculate(-100, 100);

        // Sollte nicht crashen und einen gültigen Wert liefern
        double.IsNaN(result).Should().BeFalse();
        double.IsInfinity(result).Should().BeFalse();
        result.Should().BeInRange(-1.0, 1.0);

        _output.WriteLine($"Negative BidSize (-100, 100): {result}");
    }

    [Fact]
    public void DomSnapshot_NegativeAskSize_HandledGracefully()
    {
        var calc = new StructImbCalculator();

        // AskSize = -100 → wird als 0 behandelt oder sinnvoll verarbeitet
        var result = calc.Calculate(100, -100);

        double.IsNaN(result).Should().BeFalse();
        double.IsInfinity(result).Should().BeFalse();
        result.Should().BeInRange(-1.0, 1.0);

        _output.WriteLine($"Negative AskSize (100, -100): {result}");
    }

    [Fact]
    public void DomSnapshot_NegativePrice_HandledGracefully()
    {
        var edge = EdgeFactory.Create();

        // Negative Preise sollten nicht crashen
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: -5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        );

        var action = () => edge.ProcessSnapshot(snapshot);
        action.Should().NotThrow("Negative prices should not crash");

        _output.WriteLine("Negative BidPrice: Handled gracefully");
    }

    [Fact]
    public void DomSnapshot_AskLowerThanBid_HandledGracefully()
    {
        var edge = EdgeFactory.Create();

        // AskPrice < BidPrice (ungültiger Markt) - sollte Gate failen
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5001.00m,
            AskPrice: 5000.00m,  // Ask < Bid
            BidSize: 100,
            AskSize: 100
        );

        var result = edge.ProcessSnapshot(snapshot);

        // SpreadGate sollte dies eigentlich als gültig sehen (Spread = -4 Ticks)
        // oder als ungültig ablehnen - wichtig ist: kein Crash
        result.Should().NotBeNull();

        _output.WriteLine($"Ask < Bid: GatePassed={result.IsQualityGatePassed}");
    }

    [Fact]
    public void DomSnapshot_ZeroPrice_HandledGracefully()
    {
        var gate = new SpreadQualityGate(_config);

        // BidPrice = 0
        var snapshot1 = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 0m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        );

        var action1 = () => gate.Check(snapshot1);
        action1.Should().NotThrow("Zero BidPrice should not crash");

        // AskPrice = 0
        var snapshot2 = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 0m,
            BidSize: 100,
            AskSize: 100
        );

        var action2 = () => gate.Check(snapshot2);
        action2.Should().NotThrow("Zero AskPrice should not crash");

        _output.WriteLine("Zero prices: Handled gracefully");
    }

    [Fact]
    public void Configuration_ValidDefault_Works()
    {
        var config = EdgeConfiguration.Default;

        config.WindowSize.Should().BeGreaterThan(0);
        config.MinWarmupSamples.Should().BeGreaterThan(0);
        config.MinWarmupSamples.Should().BeLessThanOrEqualTo(config.WindowSize);
        config.ZThreshold.Should().BeGreaterThan(0);
        config.ZClip.Should().BeGreaterThan(0);
        config.Epsilon.Should().BeGreaterThan(0);

        _output.WriteLine("Default configuration: VALID");
    }

    [Fact]
    public void EdgeFactory_NullConfig_UsesDefault()
    {
        // EdgeFactory.Create(null) → verwendet Default-Config
        var edge = EdgeFactory.Create(null);

        edge.Should().NotBeNull("Factory should create edge with null config");

        // Should work normally
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        );

        var result = edge.ProcessSnapshot(snapshot);
        result.Should().NotBeNull();

        _output.WriteLine("Factory with null config: Uses default");
    }

    [Fact]
    public void StructImb_BothNegative_ReturnsZero()
    {
        var calc = new StructImbCalculator();

        // Beide negativ → als 0/0 behandeln
        var result = calc.Calculate(-100, -100);

        result.Should().Be(0.0, "Both negative should return 0");

        _output.WriteLine($"Both negative (-100, -100): {result}");
    }

    [Fact]
    public void StructImb_VeryLargeValues_NoOverflow()
    {
        var calc = new StructImbCalculator();

        // Große Werte sollten nicht überlaufen
        var result = calc.Calculate(int.MaxValue / 2, int.MaxValue / 2);

        double.IsNaN(result).Should().BeFalse();
        double.IsInfinity(result).Should().BeFalse();
        result.Should().BeApproximately(0.0, 0.001);

        _output.WriteLine($"Large values: {result}");
    }

    [Fact]
    public void SpreadGate_ExtremeSpread_NoOverflow()
    {
        var gate = new SpreadQualityGate(_config);

        // Extremer Spread
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 0.01m,
            AskPrice: 10000000.00m,
            BidSize: 100,
            AskSize: 100
        );

        var action = () => gate.Check(snapshot);
        action.Should().NotThrow("Extreme spread should not crash");

        var result = gate.Check(snapshot);
        result.Should().BeFalse("Extreme spread should fail gate");

        _output.WriteLine("Extreme spread: Handled gracefully, gate FAIL");
    }

    [Fact]
    public void ZScoreCalculator_ZeroMad_HandledWithEpsilon()
    {
        var zCalc = new ZScoreCalculator(_config);

        // MAD = 0 sollte Epsilon verwenden
        var z = zCalc.Calculate(0.5, 0.5, 0.0);

        z.Should().Be(0.0, "When value = median and MAD = 0, Z should be 0");
        double.IsNaN(z).Should().BeFalse();
        double.IsInfinity(z).Should().BeFalse();

        _output.WriteLine($"Z with MAD=0: {z}");
    }

    [Fact]
    public void SignalGenerator_ExtremeZValues_HandledCorrectly()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // Z-Werte außerhalb des geklippten Bereichs
        var extremeZs = new[] { 100.0, -100.0, 1000.0, -1000.0, double.MaxValue, double.MinValue };

        foreach (var z in extremeZs)
        {
            var action = () => gen.Generate(z, baseTime, true, false, true);
            action.Should().NotThrow($"Extreme Z={z} should not crash");

            var signal = gen.Generate(z, baseTime, true, false, true);
            signal.Should().BeInRange(-1, 1, "Signal should always be -1, 0, or 1");

            gen.Reset();
        }

        _output.WriteLine("Extreme Z values: All handled correctly");
    }

    [Fact]
    public void Edge_SnapshotWithDefaults_Works()
    {
        var edge = EdgeFactory.Create();

        // Minimal Snapshot mit Defaults
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.25m,
            BidSize: 1,
            AskSize: 1
        );

        var result = edge.ProcessSnapshot(snapshot);

        result.Should().NotBeNull();
        result.Signal.Should().BeInRange(-1, 1);

        _output.WriteLine("Minimal valid snapshot: Works");
    }

    [Fact]
    public void DomSnapshot_Immutability_Verified()
    {
        // DomSnapshot ist ein Record - sollte immutable sein
        var snapshot1 = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        );

        // With-Expression erstellt neue Instanz
        var snapshot2 = snapshot1 with { BidSize = 200 };

        snapshot1.BidSize.Should().Be(100, "Original should be unchanged");
        snapshot2.BidSize.Should().Be(200, "New instance should have new value");

        _output.WriteLine("DomSnapshot immutability: VERIFIED");
    }
}
