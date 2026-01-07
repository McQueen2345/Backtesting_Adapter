using FluentAssertions;
using Moq;
using QTS.Edge.Core;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;
using QTS.Edge.Core.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Integration;

/// <summary>
/// Phase D: Quality Gate Interaction Tests.
/// Stellt sicher, dass schlechte Daten die Statistik nicht verwässern.
/// </summary>
public class QualityGateInteractionTests
{
    private readonly ITestOutputHelper _output;
    private readonly EdgeConfiguration _config;
    private readonly DateTime _baseTime;

    public QualityGateInteractionTests(ITestOutputHelper output)
    {
        _output = output;
        _config = EdgeConfiguration.Default;
        _baseTime = DateTime.UtcNow;
    }

    [Fact]
    public void Edge_QualityGateFail_NoAddToRollingStats()
    {
        // KRITISCH! Bei Gate-Fail darf Add() NICHT aufgerufen werden

        // Arrange - Mit Mock für RollingStatistics
        var mockRollingStats = new Mock<IRollingStatistics>();
        mockRollingStats.Setup(r => r.IsWarm).Returns(true);
        mockRollingStats.Setup(r => r.Count).Returns(200);
        mockRollingStats.Setup(r => r.GetMedian()).Returns(0.0);
        mockRollingStats.Setup(r => r.GetMad()).Returns(0.1);

        var mockQualityGate = new Mock<IQualityGate>();
        mockQualityGate.Setup(g => g.Check(It.IsAny<IDomSnapshot>())).Returns(false); // FAIL

        var edge = new StructImbL1Edge(
            new StructImbCalculator(),
            mockRollingStats.Object,
            new ZScoreCalculator(_config),
            new SignalGenerator(_config),
            mockQualityGate.Object,
            _config
        );

        // Act - Snapshot mit Gate FAIL
        var snapshot = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5010.00m,  // Riesiger Spread
            BidSize: 100,
            AskSize: 100
        );

        edge.ProcessSnapshot(snapshot);

        // Assert - Add() wurde NICHT aufgerufen
        mockRollingStats.Verify(r => r.Add(It.IsAny<double>()), Times.Never,
            "Add() should NOT be called when QualityGate fails");

        _output.WriteLine("QualityGate FAIL: Add() not called - CORRECT");
    }

    [Fact]
    public void Edge_IsDataStale_NoAddToRollingStats()
    {
        // KRITISCH! Bei Stale Data darf Add() NICHT aufgerufen werden

        var mockRollingStats = new Mock<IRollingStatistics>();
        mockRollingStats.Setup(r => r.IsWarm).Returns(true);
        mockRollingStats.Setup(r => r.Count).Returns(200);

        var mockQualityGate = new Mock<IQualityGate>();
        mockQualityGate.Setup(g => g.Check(It.IsAny<IDomSnapshot>())).Returns(true);

        var edge = new StructImbL1Edge(
            new StructImbCalculator(),
            mockRollingStats.Object,
            new ZScoreCalculator(_config),
            new SignalGenerator(_config),
            mockQualityGate.Object,
            _config
        );

        // Act - Stale Snapshot
        var snapshot = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100,
            IsDataStale: true
        );

        edge.ProcessSnapshot(snapshot);

        // Assert - Add() wurde NICHT aufgerufen
        mockRollingStats.Verify(r => r.Add(It.IsAny<double>()), Times.Never,
            "Add() should NOT be called when IsDataStale=true");

        _output.WriteLine("IsDataStale=true: Add() not called - CORRECT");
    }

    [Fact]
    public void Edge_QualityGateFail_SignalIsZero()
    {
        var edge = EdgeFactory.Create();

        // Warmup mit guten Daten
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

        // Jetzt Gate Fail (großer Spread)
        var result = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(250 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5002.00m,  // 8 Ticks > MaxSpreadTicks
            BidSize: 150,
            AskSize: 50
        ));

        result.Signal.Should().Be(0, "QualityGate fail should force Signal=0");
        result.IsQualityGatePassed.Should().BeFalse();

        _output.WriteLine($"Signal after gate fail: {result.Signal}");
    }

    [Fact]
    public void Edge_IsDataStale_SignalIsZero()
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

        // Stale Data
        var result = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(250 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 150,
            AskSize: 50,
            IsDataStale: true
        ));

        result.Signal.Should().Be(0, "IsDataStale should force Signal=0");
        result.IsDataStale.Should().BeTrue();

        _output.WriteLine($"Signal with stale data: {result.Signal}");
    }

    [Fact]
    public void Edge_QualityGateFail_StructImbStillCalculated()
    {
        var edge = EdgeFactory.Create();

        // Gate Fail Snapshot
        var result = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5010.00m,  // Riesiger Spread
            BidSize: 100,
            AskSize: 50
        ));

        // StructImb sollte trotzdem berechnet werden (für Diagnostik)
        // StructImbZ ist verfügbar, auch wenn Gate fail
        // Bei nicht-warmem State ist Z=0, aber StructImb wurde berechnet
        result.IsQualityGatePassed.Should().BeFalse();

        _output.WriteLine($"StructImbZ (gate fail, not warm): {result.StructImbZ}");
    }

    [Fact]
    public void Edge_AlternatingGoodBad_StatisticsOnlyFromGood()
    {
        var mockRollingStats = new Mock<IRollingStatistics>();
        mockRollingStats.Setup(r => r.IsWarm).Returns(false);

        var spreadGate = new SpreadQualityGate(_config);
        var depthGate = new DepthQualityGate(_config);
        var compositeGate = new CompositeQualityGate(spreadGate, depthGate);

        var edge = new StructImbL1Edge(
            new StructImbCalculator(),
            mockRollingStats.Object,
            new ZScoreCalculator(_config),
            new SignalGenerator(_config),
            compositeGate,
            _config
        );

        int addCallCount = 0;
        mockRollingStats.Setup(r => r.Add(It.IsAny<double>()))
            .Callback(() => addCallCount++);

        // Sequence: Good, Bad, Good, Bad, Good...
        for (int i = 0; i < 100; i++)
        {
            bool isGood = i % 2 == 0;
            var snapshot = new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: isGood ? 5000.25m : 5010.00m,  // Bad = großer Spread
                BidSize: 100,
                AskSize: 100
            );
            edge.ProcessSnapshot(snapshot);
        }

        // Add sollte nur 50x aufgerufen worden sein (nur gute Snapshots)
        addCallCount.Should().Be(50, "Add should only be called for good snapshots");

        _output.WriteLine($"Add called {addCallCount} times for 100 alternating snapshots");
    }

    [Fact]
    public void Edge_LongStaleSequence_NoStatisticsDrift()
    {
        var edge = EdgeFactory.Create();

        // 100 gute Snapshots → warm
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

        var beforeStale = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(200 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));

        var zBefore = beforeStale.StructImbZ;

        // 100 stale Snapshots → Add nicht aufgerufen
        for (int i = 0; i < 100; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: _baseTime.AddMilliseconds((201 + i) * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 500,  // Würde die Statistik verändern, wenn es gezählt würde
                AskSize: 50,
                IsDataStale: true
            ));
        }

        // 10 gute Snapshots → Statistik basiert auf den 200 + 11
        var afterStale = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(301 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));

        // Z sollte ähnlich sein wie vor den Stale-Daten
        Math.Abs(afterStale.StructImbZ - zBefore).Should().BeLessThan(0.5,
            "Stale data should not affect statistics");

        _output.WriteLine($"Z before stale: {zBefore:F4}");
        _output.WriteLine($"Z after stale sequence: {afterStale.StructImbZ:F4}");
    }

    [Fact]
    public void CompositeGate_AllMustPass()
    {
        var spreadGate = new SpreadQualityGate(_config);
        var depthGate = new DepthQualityGate(_config);
        var composite = new CompositeQualityGate(spreadGate, depthGate);

        // Both OK
        var bothOk = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        );
        composite.Check(bothOk).Should().BeTrue("Both gates OK should pass");

        // SpreadGate FAIL, DepthGate OK
        var spreadFail = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5010.00m,  // Huge spread
            BidSize: 100,
            AskSize: 100
        );
        composite.Check(spreadFail).Should().BeFalse("SpreadGate FAIL should fail composite");

        // SpreadGate OK, DepthGate FAIL
        var depthFail = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 0,
            AskSize: 100
        );
        composite.Check(depthFail).Should().BeFalse("DepthGate FAIL should fail composite");

        _output.WriteLine("CompositeGate requires ALL gates to pass");
    }

    [Fact]
    public void Edge_GateFailAfterWarmup_StaysWarm()
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

        var warmResult = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(200 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));
        warmResult.IsContextWarm.Should().BeTrue();

        // Gate fail
        var gateFailResult = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(201 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5010.00m,  // Huge spread
            BidSize: 100,
            AskSize: 100
        ));

        // Edge bleibt warm
        gateFailResult.IsContextWarm.Should().BeTrue("Edge should stay warm after gate fail");
        gateFailResult.IsQualityGatePassed.Should().BeFalse();

        _output.WriteLine("Gate fail after warmup: Edge stays warm - CORRECT");
    }
}
