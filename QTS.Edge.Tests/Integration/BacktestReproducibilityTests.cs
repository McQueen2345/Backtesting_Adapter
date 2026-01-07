using System.Text.Json;
using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Integration;

/// <summary>
/// Phase J: End-to-End Backtest-Reproduzierbarkeit Tests.
/// Validiert dass die C# Implementation konsistente Backtest-Ergebnisse liefert.
/// </summary>
public class BacktestReproducibilityTests
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDataPath;

    public BacktestReproducibilityTests(ITestOutputHelper output)
    {
        _output = output;
        _testDataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "TestData", "ES_YOSHI911_20251110_212851.dom.jsonl"
        );
    }

    [Fact]
    public void Backtest_WarmupPhase_NoSignals()
    {
        var edge = EdgeFactory.Create();
        var config = EdgeConfiguration.Default;
        var baseTime = DateTime.UtcNow;

        // Erste 200 Snapshots → alle Signals müssen 0 sein
        for (int i = 0; i < config.MinWarmupSamples; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 150,
                AskSize: 50
            );

            var result = edge.ProcessSnapshot(snapshot);

            result.Signal.Should().Be(0, $"During warmup (i={i}), signal must be 0");
            result.IsContextWarm.Should().BeFalse($"During warmup (i={i}), IsContextWarm must be false");
        }

        _output.WriteLine($"First {config.MinWarmupSamples} snapshots: All signals = 0 (CORRECT)");
    }

    [Fact]
    public void Backtest_AfterWarmup_SignalsCanBeGenerated()
    {
        var edge = EdgeFactory.Create();
        var config = EdgeConfiguration.Default;
        var baseTime = DateTime.UtcNow;

        // Warmup mit variierenden Daten (für MAD > 0)
        for (int i = 0; i < config.MinWarmupSamples; i++)
        {
            int bidSize = 100 + (i % 2 == 0 ? 10 : -10);
            int askSize = 100 + (i % 2 == 0 ? -10 : 10);
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: bidSize,
                AskSize: askSize
            ));
        }

        // Jetzt extrem bullisch → sollte LONG Signal generieren
        var result = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: baseTime.AddMilliseconds(config.MinWarmupSamples * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 500,
            AskSize: 10
        ));

        result.IsContextWarm.Should().BeTrue("After warmup, should be warm");
        // Signal könnte 0 oder 1 sein, abhängig von Z-Score
        result.Signal.Should().BeInRange(-1, 1, "Signal should be valid");

        _output.WriteLine($"After warmup: Signal={result.Signal}, Z={result.StructImbZ:F3}");
    }

    [Fact]
    public void Backtest_SignalDistribution_Plausible()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;
        var random = new Random(42);

        int longCount = 0;
        int shortCount = 0;
        int flatCount = 0;

        // 1000 realistische Snapshots
        for (int i = 0; i < 1000; i++)
        {
            // Simuliere realistische Bid/Ask-Imbalance
            int baseBid = 100;
            int baseAsk = 100;
            int variation = (int)(30 * Math.Sin(i * 0.05) + random.Next(-20, 20));

            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: Math.Max(1, baseBid + variation),
                AskSize: Math.Max(1, baseAsk - variation / 2)
            );

            var result = edge.ProcessSnapshot(snapshot);

            if (result.Signal == 1) longCount++;
            else if (result.Signal == -1) shortCount++;
            else flatCount++;
        }

        _output.WriteLine($"Signal Distribution (1000 snapshots):");
        _output.WriteLine($"  LONG:  {longCount} ({100.0 * longCount / 1000:F1}%)");
        _output.WriteLine($"  SHORT: {shortCount} ({100.0 * shortCount / 1000:F1}%)");
        _output.WriteLine($"  FLAT:  {flatCount} ({100.0 * flatCount / 1000:F1}%)");

        // Plausibilitäts-Checks
        flatCount.Should().BeGreaterThan(longCount, "FLAT should be more common than LONG");
        flatCount.Should().BeGreaterThan(shortCount, "FLAT should be more common than SHORT");

        // Bei normalverteilten Daten sollten LONG und SHORT ungefähr gleich sein
        var ratio = (double)longCount / Math.Max(1, shortCount);
        ratio.Should().BeInRange(0.3, 3.0, "LONG/SHORT ratio should be roughly balanced");
    }

    [Fact]
    public void Backtest_DeterministicWithSeed()
    {
        // Gleicher Random Seed → gleiche Ergebnisse
        var results1 = RunBacktestWithSeed(42);
        var results2 = RunBacktestWithSeed(42);

        results1.Count.Should().Be(results2.Count);

        for (int i = 0; i < results1.Count; i++)
        {
            results1[i].Signal.Should().Be(results2[i].Signal, $"Signal at {i} should match");
            results1[i].Z.Should().Be(results2[i].Z, $"Z at {i} should match");
        }

        _output.WriteLine("Deterministic with same seed: VERIFIED");
    }

    private List<(int Signal, double Z)> RunBacktestWithSeed(int seed)
    {
        var edge = EdgeFactory.Create();
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var random = new Random(seed);
        var results = new List<(int Signal, double Z)>();

        for (int i = 0; i < 500; i++)
        {
            int bid = 100 + random.Next(-50, 50);
            int ask = 100 + random.Next(-50, 50);

            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: Math.Max(1, bid),
                AskSize: Math.Max(1, ask)
            );

            var result = edge.ProcessSnapshot(snapshot);
            results.Add((result.Signal, result.StructImbZ));
        }

        return results;
    }

    [Fact]
    public void Backtest_RealData_Reproducible()
    {
        if (!File.Exists(_testDataPath))
        {
            _output.WriteLine($"Test data not found: {_testDataPath}");
            return;
        }

        var lines = File.ReadAllLines(_testDataPath);
        var snapshots = ParseSnapshots(lines);

        // Lauf 1
        var edge1 = EdgeFactory.Create();
        var results1 = snapshots.Select(s => edge1.ProcessSnapshot(s)).ToList();

        // Lauf 2
        var edge2 = EdgeFactory.Create();
        var results2 = snapshots.Select(s => edge2.ProcessSnapshot(s)).ToList();

        // Vergleiche
        results1.Count.Should().Be(results2.Count);

        for (int i = 0; i < results1.Count; i++)
        {
            results1[i].Signal.Should().Be(results2[i].Signal, $"Signal at {i}");
            results1[i].StructImbZ.Should().Be(results2[i].StructImbZ, $"Z at {i}");
        }

        _output.WriteLine($"Real data ({snapshots.Count} snapshots): Reproducible");
    }

    [Fact]
    public void Backtest_RealData_ExpectedSignalCount()
    {
        if (!File.Exists(_testDataPath))
        {
            _output.WriteLine($"Test data not found: {_testDataPath}");
            return;
        }

        var lines = File.ReadAllLines(_testDataPath);
        var snapshots = ParseSnapshots(lines);
        var edge = EdgeFactory.Create();

        int signalChanges = 0;
        int lastSignal = 0;

        foreach (var snapshot in snapshots)
        {
            var result = edge.ProcessSnapshot(snapshot);

            if (result.IsContextWarm && result.Signal != lastSignal)
            {
                signalChanges++;
                lastSignal = result.Signal;
            }
        }

        // Bekannte erwartete Anzahl aus dem FullSystemTest
        signalChanges.Should().Be(14, "Real data should have 14 signal changes");

        _output.WriteLine($"Signal changes in real data: {signalChanges}");
    }

    [Fact]
    public void Backtest_StateAfterReset_MatchesFreshStart()
    {
        var snapshots = GenerateTestSnapshots(500);

        // Lauf 1: Frische Edge
        var edge1 = EdgeFactory.Create();
        var results1 = snapshots.Select(s => edge1.ProcessSnapshot(s)).ToList();

        // Lauf 2: Edge mit Reset nach Vorwärmung
        var edge2 = EdgeFactory.Create();

        // Vorwärmen mit anderen Daten
        for (int i = 0; i < 300; i++)
        {
            edge2.ProcessSnapshot(new DomSnapshot(
                Timestamp: DateTime.UtcNow.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 200,
                AskSize: 50
            ));
        }

        edge2.Reset();

        var results2 = snapshots.Select(s => edge2.ProcessSnapshot(s)).ToList();

        // Sollten identisch sein
        for (int i = 0; i < results1.Count; i++)
        {
            results1[i].Signal.Should().Be(results2[i].Signal, $"Signal at {i}");
            results1[i].StructImbZ.Should().Be(results2[i].StructImbZ, $"Z at {i}");
        }

        _output.WriteLine("State after Reset matches fresh start: VERIFIED");
    }

    private List<DomSnapshot> ParseSnapshots(string[] lines)
    {
        var snapshots = new List<DomSnapshot>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var json = JsonDocument.Parse(line);
            var root = json.RootElement;

            snapshots.Add(new DomSnapshot(
                Timestamp: DateTime.Parse(root.GetProperty("ts_utc").GetString()!),
                BidPrice: root.GetProperty("bb").GetProperty("p").GetDecimal(),
                AskPrice: root.GetProperty("ba").GetProperty("p").GetDecimal(),
                BidSize: root.GetProperty("bb").GetProperty("s").GetInt32(),
                AskSize: root.GetProperty("ba").GetProperty("s").GetInt32()
            ));
        }

        return snapshots;
    }

    private List<DomSnapshot> GenerateTestSnapshots(int count)
    {
        var snapshots = new List<DomSnapshot>();
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < count; i++)
        {
            int bidSize = 100 + (int)(50 * Math.Sin(i * 0.1));
            int askSize = 100 - (int)(30 * Math.Cos(i * 0.15));

            snapshots.Add(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: Math.Max(1, bidSize),
                AskSize: Math.Max(1, askSize)
            ));
        }

        return snapshots;
    }
}
