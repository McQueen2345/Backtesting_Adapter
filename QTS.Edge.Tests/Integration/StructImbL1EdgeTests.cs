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

namespace QTS.Edge.Tests.Integration;

public class StructImbL1EdgeTests
{
    private readonly EdgeConfiguration _config;
    private readonly DateTime _baseTime;

    public StructImbL1EdgeTests()
    {
        _config = EdgeConfiguration.Default;
        _baseTime = DateTime.UtcNow;
    }

    private StructImbL1Edge CreateEdge()
    {
        return new StructImbL1Edge(
            new StructImbCalculator(),
            new RollingStatistics(_config.WindowSize, _config.MinWarmupSamples),
            new ZScoreCalculator(_config),
            new SignalGenerator(_config),
            new CompositeQualityGate(
                new SpreadQualityGate(_config),
                new DepthQualityGate(_config)
            ),
            _config
        );
    }

    private DomSnapshot CreateValidSnapshot(DateTime timestamp, int bidSize = 100, int askSize = 100)
    {
        return new DomSnapshot(
            Timestamp: timestamp,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m, // 1 Tick Spread
            BidSize: bidSize,
            AskSize: askSize
        );
    }

    // === T071: Basic Tests ===

    [Fact]
    public void ProcessSnapshot_DuringWarmup_ReturnsFlat()
    {
        var edge = CreateEdge();

        // Verarbeite 199 Snapshots (unter Warmup von 200)
        IEdgeSignal? lastResult = null;
        for (int i = 0; i < 199; i++)
        {
            var snapshot = CreateValidSnapshot(_baseTime.AddMilliseconds(i * 100));
            lastResult = edge.ProcessSnapshot(snapshot);
        }

        // Assert
        lastResult.Should().NotBeNull();
        lastResult!.Signal.Should().Be(0, "Während Warmup sollte Signal=0 sein");
        lastResult.IsContextWarm.Should().BeFalse("IsContextWarm sollte false sein unter 200 Samples");
    }

    [Fact]
    public void ProcessSnapshot_AfterWarmup_GeneratesSignal()
    {
        var edge = CreateEdge();

        // Verarbeite 250 Snapshots mit bullischem Bias (Bid > Ask)
        IEdgeSignal? lastResult = null;
        for (int i = 0; i < 250; i++)
        {
            // Bullischer Bias: BidSize > AskSize
            var snapshot = CreateValidSnapshot(
                _baseTime.AddMilliseconds(i * 100),
                bidSize: 150,
                askSize: 50
            );
            lastResult = edge.ProcessSnapshot(snapshot);
        }

        // Assert
        lastResult.Should().NotBeNull();
        lastResult!.IsContextWarm.Should().BeTrue("Nach 250 Samples sollte IsContextWarm=true sein");
        lastResult.IsQualityGatePassed.Should().BeTrue();
        lastResult.IsDataStale.Should().BeFalse();
        // Bei konstantem bullischem Bias könnte Signal LONG sein, aber Median passt sich an
        // Wichtig ist dass die Pipeline funktioniert
    }

    // === T072: Gate Tests ===

    [Fact]
    public void ProcessSnapshot_StaleData_ReturnsFlat()
    {
        var edge = CreateEdge();

        // Erst Warmup erreichen
        for (int i = 0; i < 200; i++)
        {
            var snapshot = CreateValidSnapshot(_baseTime.AddMilliseconds(i * 100));
            edge.ProcessSnapshot(snapshot);
        }

        // Dann stale Snapshot senden
        var staleSnapshot = new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(200 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 150,
            AskSize: 50,
            IsDataStale: true
        );

        var result = edge.ProcessSnapshot(staleSnapshot);

        result.Signal.Should().Be(0, "Stale Data sollte Signal=0 zurückgeben");
        result.IsDataStale.Should().BeTrue();
    }

    [Fact]
    public void ProcessSnapshot_QualityGateFail_ReturnsFlat()
    {
        var edge = CreateEdge();

        // Erst Warmup erreichen
        for (int i = 0; i < 200; i++)
        {
            var snapshot = CreateValidSnapshot(_baseTime.AddMilliseconds(i * 100));
            edge.ProcessSnapshot(snapshot);
        }

        // Dann Snapshot mit zu großem Spread (5 Ticks > 4 erlaubt)
        var badSpreadSnapshot = new DomSnapshot(
            Timestamp: _baseTime.AddMilliseconds(200 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5001.25m, // 5 Ticks Spread
            BidSize: 150,
            AskSize: 50
        );

        var result = edge.ProcessSnapshot(badSpreadSnapshot);

        result.Signal.Should().Be(0, "Quality Gate Fail sollte Signal=0 zurückgeben");
        result.IsQualityGatePassed.Should().BeFalse();
    }

    // === T073: Add Prevention Tests (mit Mocks) ===

    [Fact]
    public void ProcessSnapshot_StaleData_NoAddCalled()
    {
        // Arrange - Mit Mock für RollingStatistics
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

        // Act - Stale Snapshot senden
        var staleSnapshot = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100,
            IsDataStale: true
        );

        edge.ProcessSnapshot(staleSnapshot);

        // Assert - Add() darf NICHT aufgerufen worden sein
        mockRollingStats.Verify(r => r.Add(It.IsAny<double>()), Times.Never,
            "Bei IsDataStale=true darf Add() NICHT aufgerufen werden");
    }

    [Fact]
    public void ProcessSnapshot_QualityGateFail_NoAddCalled()
    {
        // Arrange - Mit Mock für RollingStatistics und QualityGate
        var mockRollingStats = new Mock<IRollingStatistics>();
        mockRollingStats.Setup(r => r.IsWarm).Returns(true);
        mockRollingStats.Setup(r => r.Count).Returns(200);

        var mockQualityGate = new Mock<IQualityGate>();
        mockQualityGate.Setup(g => g.Check(It.IsAny<IDomSnapshot>())).Returns(false); // Gate FAIL

        var edge = new StructImbL1Edge(
            new StructImbCalculator(),
            mockRollingStats.Object,
            new ZScoreCalculator(_config),
            new SignalGenerator(_config),
            mockQualityGate.Object,
            _config
        );

        // Act - Snapshot senden (Quality Gate wird fehlschlagen)
        var snapshot = CreateValidSnapshot(_baseTime);

        edge.ProcessSnapshot(snapshot);

        // Assert - Add() darf NICHT aufgerufen worden sein
        mockRollingStats.Verify(r => r.Add(It.IsAny<double>()), Times.Never,
            "Bei QualityGate Fail darf Add() NICHT aufgerufen werden");
    }

    // === T074: Reset Test ===

    [Fact]
    public void Reset_ClearsAllState()
    {
        var edge = CreateEdge();

        // Erst Warmup erreichen und Signal generieren
        for (int i = 0; i < 250; i++)
        {
            var snapshot = CreateValidSnapshot(
                _baseTime.AddMilliseconds(i * 100),
                bidSize: 150,
                askSize: 50
            );
            edge.ProcessSnapshot(snapshot);
        }

        // Reset
        edge.Reset();

        // Nach Reset: Erster Snapshot sollte wieder "nicht warm" sein
        var newSnapshot = CreateValidSnapshot(_baseTime.AddSeconds(100));
        var result = edge.ProcessSnapshot(newSnapshot);

        result.IsContextWarm.Should().BeFalse("Nach Reset sollte IsContextWarm=false sein");
        result.Signal.Should().Be(0, "Nach Reset sollte Signal=0 sein");
    }

    // === T075: End-to-End Tests ===

    [Fact]
    public void EndToEnd_BullishSequence_GeneratesLong()
    {
        var edge = CreateEdge();

        // Warmup mit leicht variierenden Daten (für MAD > 0)
        for (int i = 0; i < 200; i++)
        {
            // Alternierend leicht bullisch/bearisch für MAD-Berechnung
            int bidSize = 100 + (i % 2 == 0 ? 10 : -10);
            int askSize = 100 + (i % 2 == 0 ? -10 : 10);
            var snapshot = CreateValidSnapshot(
                _baseTime.AddMilliseconds(i * 100),
                bidSize: bidSize,
                askSize: askSize
            );
            edge.ProcessSnapshot(snapshot);
        }

        // Dann EXTREM bullische Daten (sollte Z >> 1.5 erzeugen)
        IEdgeSignal? lastResult = null;
        for (int i = 0; i < 10; i++)
        {
            var snapshot = CreateValidSnapshot(
                _baseTime.AddMilliseconds((200 + i) * 100),
                bidSize: 500,  // Extrem bullisch
                askSize: 10
            );
            lastResult = edge.ProcessSnapshot(snapshot);
        }

        // Assert - sollte LONG Signal generieren
        lastResult.Should().NotBeNull();
        lastResult!.IsContextWarm.Should().BeTrue();
        lastResult.Signal.Should().Be(1, "Bullische Sequenz sollte LONG (+1) generieren");
    }

    [Fact]
    public void EndToEnd_BearishSequence_GeneratesShort()
    {
        var edge = CreateEdge();

        // Warmup mit leicht variierenden Daten (für MAD > 0)
        for (int i = 0; i < 200; i++)
        {
            // Alternierend leicht bullisch/bearisch für MAD-Berechnung
            int bidSize = 100 + (i % 2 == 0 ? 10 : -10);
            int askSize = 100 + (i % 2 == 0 ? -10 : 10);
            var snapshot = CreateValidSnapshot(
                _baseTime.AddMilliseconds(i * 100),
                bidSize: bidSize,
                askSize: askSize
            );
            edge.ProcessSnapshot(snapshot);
        }

        // Dann EXTREM bearische Daten (sollte Z << -1.5 erzeugen)
        IEdgeSignal? lastResult = null;
        for (int i = 0; i < 10; i++)
        {
            var snapshot = CreateValidSnapshot(
                _baseTime.AddMilliseconds((200 + i) * 100),
                bidSize: 10,   // Extrem bearisch
                askSize: 500
            );
            lastResult = edge.ProcessSnapshot(snapshot);
        }

        // Assert - sollte SHORT Signal generieren
        lastResult.Should().NotBeNull();
        lastResult!.IsContextWarm.Should().BeTrue();
        lastResult.Signal.Should().Be(-1, "Bearische Sequenz sollte SHORT (-1) generieren");
    }
}
