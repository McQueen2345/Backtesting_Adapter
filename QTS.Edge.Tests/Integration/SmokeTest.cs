using FluentAssertions;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Models;
using QTS.Edge.Core.Statistics;
using Xunit;

namespace QTS.Edge.Tests.Integration;

/// <summary>
/// Smoke Test: Prüft ob alle Komponenten zusammenarbeiten.
/// </summary>
public class SmokeTest
{
    [Fact]
    public void FullPipeline_SimulatedData_ProducesValidZScore()
    {
        // Arrange - Alle Komponenten erstellen
        var config = EdgeConfiguration.Default;
        var structImbCalc = new StructImbCalculator();
        var rollingStats = new RollingStatistics(config.WindowSize, config.MinWarmupSamples);
        var zScoreCalc = new ZScoreCalculator(config);
        var spreadGate = new SpreadQualityGate(config);
        var depthGate = new DepthQualityGate(config);
        var compositeGate = new CompositeQualityGate(spreadGate, depthGate);

        // Act - Simuliere 250 DOM-Snapshots (Warmup = 200)
        var timestamp = DateTime.UtcNow;
        double lastZ = 0;

        for (int i = 0; i < 250; i++)
        {
            // Simuliere leicht bullishes Orderbook (Bid > Ask)
            int bidSize = 100 + (i % 50);  // 100-149
            int askSize = 80 + (i % 30);   // 80-109

            var snapshot = new DomSnapshot(
                Timestamp: timestamp.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,  // 1 Tick Spread
                BidSize: bidSize,
                AskSize: askSize
            );

            // Quality Gate prüfen
            bool qualityOk = compositeGate.Check(snapshot);
            qualityOk.Should().BeTrue($"Snapshot {i} sollte Quality Gate passieren");

            // StructImb berechnen
            double structImb = structImbCalc.Calculate(bidSize, askSize);

            // Zur Statistik hinzufügen
            rollingStats.Add(structImb);

            // Nach Warmup: Z-Score berechnen
            if (rollingStats.IsWarm)
            {
                double median = rollingStats.GetMedian();
                double mad = rollingStats.GetMad();
                lastZ = zScoreCalc.Calculate(structImb, median, mad);
            }
        }

        // Assert
        rollingStats.IsWarm.Should().BeTrue("Nach 250 Samples sollte IsWarm=true sein");
        rollingStats.Count.Should().Be(250);

        // Z-Score sollte ein valider Wert sein (zwischen -5 und +5)
        lastZ.Should().BeInRange(-5.0, 5.0);

        // Da wir konsistent bullish sind (Bid > Ask), sollte StructImb positiv sein
        var finalStructImb = structImbCalc.Calculate(149, 109);
        finalStructImb.Should().BePositive();
    }

    [Fact]
    public void QualityGates_RejectBadData()
    {
        var config = EdgeConfiguration.Default;
        var compositeGate = new CompositeQualityGate(
            new SpreadQualityGate(config),
            new DepthQualityGate(config)
        );

        // Zu großer Spread (5 Ticks > 4 erlaubt)
        var badSpread = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5001.25m,  // 5 Ticks
            BidSize: 100,
            AskSize: 100
        );
        compositeGate.Check(badSpread).Should().BeFalse("Spread zu groß");

        // Keine Depth
        var noDepth = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 0,
            AskSize: 100
        );
        compositeGate.Check(noDepth).Should().BeFalse("Keine Bid-Depth");
    }

    [Fact]
    public void RollingStatistics_MedianAndMad_AreConsistent()
    {
        var stats = new RollingStatistics(100, 10);

        // Füge bekannte Werte hinzu
        double[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        foreach (var v in values)
        {
            stats.Add(v);
        }

        // Lower Median von [1,2,3,4,5,6,7,8,9,10] = 5 (Index 4)
        stats.GetMedian().Should().Be(5.0);

        // MAD: Abweichungen vom Median 5: [4,3,2,1,0,1,2,3,4,5]
        // Sortiert: [0,1,1,2,2,3,3,4,4,5] → Lower Median = 2
        stats.GetMad().Should().Be(2.0);
    }
}
