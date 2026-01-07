using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Timing;

/// <summary>
/// Phase F: Timing & Stale-Detection Tests.
/// Testet das Verhalten bei Datenlücken und Stale-Detection.
/// </summary>
public class StaleDetectionTests
{
    private readonly ITestOutputHelper _output;
    private readonly EdgeConfiguration _config;
    private readonly DateTime _baseTime;

    public StaleDetectionTests(ITestOutputHelper output)
    {
        _output = output;
        _config = EdgeConfiguration.Default;
        _baseTime = DateTime.UtcNow;
    }

    [Fact]
    public void Stale_FlagFalse_ProcessedNormally()
    {
        var edge = EdgeFactory.Create();

        var snapshot = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100,
            IsDataStale: false
        );

        var result = edge.ProcessSnapshot(snapshot);

        result.IsDataStale.Should().BeFalse();

        _output.WriteLine("IsDataStale=false: Processed normally");
    }

    [Fact]
    public void Stale_FlagTrue_ReturnsZeroSignal()
    {
        var edge = EdgeFactory.Create();

        // Warmup
        for (int i = 0; i < 250; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 150,
                AskSize: 50
            ));
        }

        // Stale snapshot
        var result = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(250 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 150,
            AskSize: 50,
            IsDataStale: true
        ));

        result.IsDataStale.Should().BeTrue();
        result.Signal.Should().Be(0, "Stale data should return Signal=0");

        _output.WriteLine($"Stale snapshot: Signal={result.Signal}");
    }

    [Fact]
    public void Stale_RecoveryToNormal()
    {
        var edge = EdgeFactory.Create();

        // Warmup
        for (int i = 0; i < 200; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));
        }

        // Normal → Warm
        var normal1 = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(200 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));
        normal1.IsContextWarm.Should().BeTrue();

        // Stale phase
        for (int i = 0; i < 10; i++)
        {
            var stale = edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds((201 + i) * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100,
                IsDataStale: true
            ));
            stale.IsDataStale.Should().BeTrue();
            stale.Signal.Should().Be(0);
        }

        // Recovery to normal
        var recovery = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(211 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100,
            IsDataStale: false
        ));

        recovery.IsDataStale.Should().BeFalse();
        recovery.IsContextWarm.Should().BeTrue("Should still be warm after recovery");

        _output.WriteLine("Stale → Recovery: System works normally again");
    }

    [Fact]
    public void Stale_NoStatisticsDuringStale()
    {
        var edge = EdgeFactory.Create();

        // Warmup mit neutralen Daten
        for (int i = 0; i < 200; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));
        }

        // Z-Score vor Stale-Phase
        var beforeStale = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(200 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));
        var zBefore = beforeStale.StructImbZ;

        // Stale-Phase mit EXTREM anderen Werten
        // Diese würden die Statistik massiv verändern, wenn sie gezählt würden
        for (int i = 0; i < 50; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds((201 + i) * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 1000,  // Extrem bullisch
                AskSize: 1,
                IsDataStale: true
            ));
        }

        // Nach Stale mit normalen Daten
        var afterStale = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(251 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));

        // Z sollte ähnlich sein wie vorher (Stale-Daten wurden nicht gezählt)
        Math.Abs(afterStale.StructImbZ - zBefore).Should().BeLessThan(0.5,
            "Stale data should not affect statistics");

        _output.WriteLine($"Z before stale: {zBefore:F4}");
        _output.WriteLine($"Z after stale (with extreme data ignored): {afterStale.StructImbZ:F4}");
    }

    [Fact]
    public void Timing_100msInterval_Consistent()
    {
        var edge = EdgeFactory.Create();
        var results = new List<IEdgeSignal>();

        // Simuliere 100ms Timer-Ticks
        for (int i = 0; i < 300; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100 + (i % 20),
                AskSize: 100
            );

            results.Add(edge.ProcessSnapshot(snapshot));
        }

        // Alle sollten verarbeitet worden sein
        results.Count.Should().Be(300);

        // Ab Snapshot 200 sollten alle warm sein
        results.Skip(199).All(r => r.IsContextWarm).Should().BeTrue();

        _output.WriteLine($"Processed {results.Count} snapshots at 100ms intervals");
    }

    [Fact]
    public void Timing_IrregularIntervals_StillWorks()
    {
        var edge = EdgeFactory.Create();

        // Unregelmäßige Intervalle
        var intervals = new[] { 50, 150, 80, 200, 90, 100, 300, 50, 100 };
        var currentTime = _baseTime;

        for (int i = 0; i < 300; i++)
        {
            var interval = intervals[i % intervals.Length];
            currentTime = currentTime.AddMilliseconds(interval);

            var snapshot = new DomSnapshot(
                Timestamp: currentTime,
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            );

            var result = edge.ProcessSnapshot(snapshot);

            // Sollte nicht crashen
            result.Should().NotBeNull();
        }

        _output.WriteLine("Irregular intervals: All snapshots processed");
    }

    [Fact]
    public void Timing_LargeGap_SystemContinues()
    {
        var edge = EdgeFactory.Create();

        // Warmup
        for (int i = 0; i < 200; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));
        }

        // Große Lücke (10 Sekunden)
        var afterGap = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddSeconds(30),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));

        // System sollte weitermachen
        afterGap.IsContextWarm.Should().BeTrue("System should stay warm after gap");

        _output.WriteLine("10s gap: System continues normally");
    }

    [Fact]
    public void Stale_MultipleStaleThenRecovery_WorksCorrectly()
    {
        var edge = EdgeFactory.Create();

        // Warmup
        for (int i = 0; i < 200; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));
        }

        // Mehrere Stale/Recovery-Zyklen
        for (int cycle = 0; cycle < 5; cycle++)
        {
            // Stale phase
            for (int i = 0; i < 5; i++)
            {
                var stale = edge.ProcessSnapshot(new DomSnapshot(
                    Timestamp: _baseTime.AddMilliseconds((200 + cycle * 10 + i) * 100),
                    BidPrice: 5000.00m,
                    AskPrice: 5000.25m,
                    BidSize: 100,
                    AskSize: 100,
                    IsDataStale: true
                ));
                stale.Signal.Should().Be(0);
            }

            // Recovery
            for (int i = 0; i < 5; i++)
            {
                var normal = edge.ProcessSnapshot(new DomSnapshot(
                    Timestamp: _baseTime.AddMilliseconds((205 + cycle * 10 + i) * 100),
                    BidPrice: 5000.00m,
                    AskPrice: 5000.25m,
                    BidSize: 100,
                    AskSize: 100
                ));
                normal.IsContextWarm.Should().BeTrue();
            }
        }

        _output.WriteLine("5 Stale/Recovery cycles: All handled correctly");
    }
}
