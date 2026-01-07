using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;
using QTS.Edge.Core.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Concurrency;

/// <summary>
/// Phase H: Concurrency-Sicherheit Tests.
/// Dokumentiert das erwartete Threading-Verhalten.
/// HINWEIS: Edge ist by-design NICHT thread-safe!
/// </summary>
public class ThreadSafetyTests
{
    private readonly ITestOutputHelper _output;
    private readonly EdgeConfiguration _config;

    public ThreadSafetyTests(ITestOutputHelper output)
    {
        _output = output;
        _config = EdgeConfiguration.Default;
    }

    [Fact]
    public void Edge_SingleThreaded_ByDesign()
    {
        // Dokumentations-Test:
        // Edge.ProcessSnapshot() muss von einem einzelnen Thread aufgerufen werden
        // Kein internes Locking (Performance-Grund)

        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Single-threaded usage works perfectly
        for (int i = 0; i < 1000; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            );

            var result = edge.ProcessSnapshot(snapshot);
            result.Should().NotBeNull();
        }

        _output.WriteLine("Single-threaded usage: WORKS");
        _output.WriteLine("NOTE: Multi-threaded usage is NOT supported by design");
    }

    [Fact]
    public void Edge_MultipleInstances_Independent()
    {
        // Zwei Edge-Instanzen in verschiedenen Threads
        // Beeinflussen sich NICHT gegenseitig

        var results1 = new List<double>();
        var results2 = new List<double>();

        var t1 = Task.Run(() =>
        {
            var edge = EdgeFactory.Create();
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            for (int i = 0; i < 500; i++)
            {
                var snapshot = new DomSnapshot(
                    Timestamp: baseTime.AddMilliseconds(i * 100),
                    BidPrice: 5000.00m,
                    AskPrice: 5000.25m,
                    BidSize: 150,  // Bullish
                    AskSize: 50
                );

                var result = edge.ProcessSnapshot(snapshot);
                if (result.IsContextWarm)
                {
                    lock (results1)
                    {
                        results1.Add(result.StructImbZ);
                    }
                }
            }
        });

        var t2 = Task.Run(() =>
        {
            var edge = EdgeFactory.Create();
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            for (int i = 0; i < 500; i++)
            {
                var snapshot = new DomSnapshot(
                    Timestamp: baseTime.AddMilliseconds(i * 100),
                    BidPrice: 5000.00m,
                    AskPrice: 5000.25m,
                    BidSize: 50,   // Bearish (different from thread 1)
                    AskSize: 150
                );

                var result = edge.ProcessSnapshot(snapshot);
                if (result.IsContextWarm)
                {
                    lock (results2)
                    {
                        results2.Add(result.StructImbZ);
                    }
                }
            }
        });

        Task.WaitAll(t1, t2);

        // Results should be different (different inputs)
        results1.Count.Should().BeGreaterThan(0);
        results2.Count.Should().BeGreaterThan(0);

        // Thread 1 (bullish) should have positive Z scores
        // Thread 2 (bearish) should have negative Z scores
        var avg1 = results1.Average();
        var avg2 = results2.Average();

        avg1.Should().BeGreaterThan(0, "Bullish edge should have positive Z");
        avg2.Should().BeLessThan(0, "Bearish edge should have negative Z");

        _output.WriteLine($"Thread 1 (bullish) avg Z: {avg1:F4}");
        _output.WriteLine($"Thread 2 (bearish) avg Z: {avg2:F4}");
        _output.WriteLine("Multiple instances in different threads: INDEPENDENT");
    }

    [Fact]
    public void RollingStatistics_NotThreadSafe_Documented()
    {
        // Dokumentations-Test:
        // Parallel Add() aufrufen → undefiniertes Verhalten
        // (Dieser Test dokumentiert, dass das NICHT unterstützt wird)

        var stats = new RollingStatistics(100, 10);

        // Single-threaded works fine
        for (int i = 0; i < 50; i++)
        {
            stats.Add(i * 0.1);
        }

        stats.Count.Should().Be(50);
        stats.IsWarm.Should().BeTrue();

        _output.WriteLine("RollingStatistics single-threaded: WORKS");
        _output.WriteLine("NOTE: Parallel Add() calls are NOT supported");
    }

    [Fact]
    public void Factory_CreateMultiple_NoSharedState()
    {
        // EdgeFactory.Create() mehrfach aufrufen
        // Jede Instanz hat eigenen State
        // Keine statischen/geteilten Variablen

        var edges = new List<IStructImbL1Edge>();
        var baseTime = DateTime.UtcNow;

        // Create 10 edges
        for (int i = 0; i < 10; i++)
        {
            edges.Add(EdgeFactory.Create());
        }

        // Modify each differently
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 250; j++)
            {
                edges[i].ProcessSnapshot(new DomSnapshot(
                    Timestamp: baseTime.AddMilliseconds(j * 100),
                    BidPrice: 5000.00m,
                    AskPrice: 5000.25m,
                    BidSize: 100 + i * 10,  // Different for each edge
                    AskSize: 100
                ));
            }
        }

        // Each should have different Z-scores now
        var zScores = edges.Select(e => e.ProcessSnapshot(new DomSnapshot(
            Timestamp: baseTime.AddMilliseconds(250 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        )).StructImbZ).ToList();

        // Z-scores should be different (different history)
        zScores.Distinct().Count().Should().BeGreaterThan(1, "Each edge should have independent state");

        _output.WriteLine($"Created {edges.Count} independent edges");
        _output.WriteLine("Factory creates independent instances: CONFIRMED");
    }

    [Fact]
    public void Edge_ResetOneDoesNotAffectOthers()
    {
        var edge1 = EdgeFactory.Create();
        var edge2 = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Both warm up
        for (int i = 0; i < 250; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            );
            edge1.ProcessSnapshot(snapshot);
            edge2.ProcessSnapshot(snapshot);
        }

        // Both should be warm
        var r1Before = edge1.ProcessSnapshot(new DomSnapshot(
            Timestamp: baseTime.AddMilliseconds(250 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));
        var r2Before = edge2.ProcessSnapshot(new DomSnapshot(
            Timestamp: baseTime.AddMilliseconds(250 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));

        r1Before.IsContextWarm.Should().BeTrue();
        r2Before.IsContextWarm.Should().BeTrue();

        // Reset edge1 only
        edge1.Reset();

        // Edge1 should be cold, edge2 still warm
        var r1After = edge1.ProcessSnapshot(new DomSnapshot(
            Timestamp: baseTime.AddSeconds(100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));
        var r2After = edge2.ProcessSnapshot(new DomSnapshot(
            Timestamp: baseTime.AddSeconds(100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));

        r1After.IsContextWarm.Should().BeFalse("Edge1 reset should make it cold");
        r2After.IsContextWarm.Should().BeTrue("Edge2 should remain warm");

        _output.WriteLine("Reset one edge does not affect others: CONFIRMED");
    }
}
