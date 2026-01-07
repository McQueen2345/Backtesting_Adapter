using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;
using QTS.Edge.Core.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Determinism;

/// <summary>
/// Phase A: Determinismus & State-Isolation Tests.
/// Stellt sicher, dass das System reproduzierbare Ergebnisse liefert.
/// </summary>
public class DeterminismTests
{
    private readonly ITestOutputHelper _output;

    public DeterminismTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Edge_SameInputs_ProduceIdenticalOutputs()
    {
        // Arrange - Zwei unabhängige Edge-Instanzen
        var edge1 = EdgeFactory.Create();
        var edge2 = EdgeFactory.Create();
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act - Füttere beide mit exakt gleichen 500 Snapshots
        for (int i = 0; i < 500; i++)
        {
            int bidSize = 100 + (int)(50 * Math.Sin(i * 0.1));
            int askSize = 100 - (int)(30 * Math.Cos(i * 0.15));

            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: Math.Max(1, bidSize),
                AskSize: Math.Max(1, askSize)
            );

            var result1 = edge1.ProcessSnapshot(snapshot);
            var result2 = edge2.ProcessSnapshot(snapshot);

            // Assert - Jeder einzelne Output muss identisch sein
            result1.Signal.Should().Be(result2.Signal, $"Signal at {i} differs");
            result1.StructImbZ.Should().Be(result2.StructImbZ, $"StructImbZ at {i} differs");
            result1.IsContextWarm.Should().Be(result2.IsContextWarm, $"IsContextWarm at {i} differs");
            result1.IsQualityGatePassed.Should().Be(result2.IsQualityGatePassed, $"IsQualityGatePassed at {i} differs");
        }

        _output.WriteLine("500 Snapshots: Alle Outputs identisch zwischen zwei Instanzen");
    }

    [Fact]
    public void Edge_MultipleRuns_ProduceSameResults()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var snapshots = new List<DomSnapshot>();

        for (int i = 0; i < 300; i++)
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

        // Act - Lauf 1
        var edge = EdgeFactory.Create();
        var results1 = new List<(int Signal, double Z)>();
        foreach (var snapshot in snapshots)
        {
            var result = edge.ProcessSnapshot(snapshot);
            results1.Add((result.Signal, result.StructImbZ));
        }

        // Reset
        edge.Reset();

        // Act - Lauf 2
        var results2 = new List<(int Signal, double Z)>();
        foreach (var snapshot in snapshots)
        {
            var result = edge.ProcessSnapshot(snapshot);
            results2.Add((result.Signal, result.StructImbZ));
        }

        // Assert
        results1.Count.Should().Be(results2.Count);
        for (int i = 0; i < results1.Count; i++)
        {
            results1[i].Signal.Should().Be(results2[i].Signal, $"Signal differs at {i}");
            results1[i].Z.Should().Be(results2[i].Z, $"Z differs at {i}");
        }

        _output.WriteLine("Zwei Läufe mit Reset dazwischen: Identische Ergebnisse");
    }

    [Fact]
    public void RollingStatistics_MedianMad_FromSameSortedVector()
    {
        // Arrange
        var config = EdgeConfiguration.Default;
        var stats = new RollingStatistics(config.WindowSize, config.MinWarmupSamples);

        // Füge 1000 Werte hinzu
        var random = new Random(42);
        for (int i = 0; i < 1000; i++)
        {
            stats.Add(random.NextDouble() * 2 - 1);
        }

        // Act - Rufe GetMedian() und GetMad() 10x hintereinander auf
        var medians = new List<double>();
        var mads = new List<double>();

        for (int i = 0; i < 10; i++)
        {
            medians.Add(stats.GetMedian());
            mads.Add(stats.GetMad());
        }

        // Assert - Alle Aufrufe müssen identische Werte liefern
        medians.Distinct().Count().Should().Be(1, "Median should be consistent across calls");
        mads.Distinct().Count().Should().Be(1, "MAD should be consistent across calls");

        _output.WriteLine($"Median (10x): {medians[0]:F6}");
        _output.WriteLine($"MAD (10x): {mads[0]:F6}");
    }

    [Fact]
    public void RollingStatistics_LowerMedian_ForEvenCount()
    {
        // Arrange
        var stats = new RollingStatistics(100, 2);

        // Füge [1, 2, 3, 4] hinzu
        stats.Add(1.0);
        stats.Add(2.0);
        stats.Add(3.0);
        stats.Add(4.0);

        // Act
        var median = stats.GetMedian();

        // Assert - Bei gerader Anzahl: LOWER Median, nicht interpoliert!
        median.Should().Be(2.0, "Lower median for even count should be 2, not 2.5");

        _output.WriteLine($"Median of [1,2,3,4]: {median} (expected: 2)");
    }

    [Fact]
    public void Reset_ClearsAllState_Completely()
    {
        // Arrange
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Fülle Edge mit 500 Snapshots (warm, hat Signal)
        for (int i = 0; i < 500; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 200,  // Bullisch
                AskSize: 50
            );
            edge.ProcessSnapshot(snapshot);
        }

        // Act - Reset
        edge.Reset();

        // Prüfe nach Reset
        var afterReset = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: baseTime.AddSeconds(100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));

        // Assert
        afterReset.IsContextWarm.Should().BeFalse("After reset, IsContextWarm should be false");
        afterReset.Signal.Should().Be(0, "After reset, Signal should be 0");

        _output.WriteLine("Reset clears all state completely - PASSED");
    }

    [Fact]
    public void Reset_RollingStatistics_IsClean()
    {
        // Arrange
        var config = EdgeConfiguration.Default;
        var stats = new RollingStatistics(config.WindowSize, config.MinWarmupSamples);

        // Add 1000 Werte
        for (int i = 0; i < 1000; i++)
        {
            stats.Add(i * 0.001);
        }

        stats.Count.Should().Be(1000);
        stats.IsWarm.Should().BeTrue();

        // Act - Reset
        stats.Reset();

        // Assert
        stats.Count.Should().Be(0, "After reset, Count should be 0");
        stats.IsWarm.Should().BeFalse("After reset, IsWarm should be false");

        // GetMedian() und GetMad() sollten 0 zurückgeben bei leerem Buffer
        stats.GetMedian().Should().Be(0, "Median of empty buffer should be 0");
        stats.GetMad().Should().Be(0, "MAD of empty buffer should be 0");

        _output.WriteLine("RollingStatistics.Reset() clears all state - PASSED");
    }

    [Fact]
    public void Reset_SignalGenerator_IsClean()
    {
        // Arrange
        var config = EdgeConfiguration.Default;
        var gen = new SignalGenerator(config);
        var baseTime = DateTime.UtcNow;

        // Generiere LONG Signal
        gen.Generate(1.8, baseTime, true, false, true);
        gen.CurrentSignal.Should().Be(1);
        gen.SignalTimestamp.Should().NotBeNull();

        // Act - Reset
        gen.Reset();

        // Assert
        gen.CurrentSignal.Should().Be(0, "After reset, CurrentSignal should be 0");
        gen.SignalTimestamp.Should().BeNull("After reset, SignalTimestamp should be null");

        // Nächster Signal-Wechsel braucht KEINEN Cooldown
        var newSignal = gen.Generate(-1.8, baseTime.AddMilliseconds(1), true, false, true);
        newSignal.Should().Be(-1, "After reset, signal change should not require cooldown");

        _output.WriteLine("SignalGenerator.Reset() clears all state - PASSED");
    }

    [Fact]
    public void Edge_OldDataHasNoInfluence_AfterReset()
    {
        // Arrange
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Phase 1: Extrem bullische Daten (500 Snapshots)
        for (int i = 0; i < 500; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 500,  // Sehr bullisch
                AskSize: 10
            ));
        }

        // Reset
        edge.Reset();

        // Phase 2: Neutrale Daten (200 Snapshots für Warmup)
        IEdgeSignal? lastResult = null;
        for (int i = 0; i < 200; i++)
        {
            lastResult = edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddSeconds(100).AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));
        }

        // Assert - Alte bullische Daten dürfen keinen Einfluss haben
        // Bei neutralen Daten sollte Z nahe 0 sein
        lastResult!.IsContextWarm.Should().BeTrue();
        Math.Abs(lastResult.StructImbZ).Should().BeLessThan(0.5,
            "After reset with neutral data, Z should be near 0");

        _output.WriteLine($"Z after reset with neutral data: {lastResult.StructImbZ:F4}");
    }
}
