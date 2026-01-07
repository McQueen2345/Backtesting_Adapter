using System.Text.Json;
using FluentAssertions;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Models;
using QTS.Edge.Core.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Integration;

/// <summary>
/// Smoke Test mit echten ES Futures DOM-Daten.
/// </summary>
public class RealDataSmokeTest
{
    private readonly ITestOutputHelper _output;

    public RealDataSmokeTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RealData_FullPipeline_ProducesValidResults()
    {
        // Arrange
        var config = EdgeConfiguration.Default;
        var structImbCalc = new StructImbCalculator();
        var rollingStats = new RollingStatistics(config.WindowSize, config.MinWarmupSamples);
        var zScoreCalc = new ZScoreCalculator(config);
        var compositeGate = new CompositeQualityGate(
            new SpreadQualityGate(config),
            new DepthQualityGate(config)
        );

        // Testdaten laden (Dateiname mit Punkt statt Unterstrich)
        var testDataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "TestData", "ES_YOSHI911_20251110_212851.dom.jsonl"
        );

        var lines = File.ReadAllLines(testDataPath);
        _output.WriteLine($"Geladene Datenpunkte: {lines.Length}");

        int processed = 0;
        int qualityPassed = 0;
        int qualityFailed = 0;
        double lastZ = 0;
        double minZ = double.MaxValue;
        double maxZ = double.MinValue;

        // Act - Verarbeite alle DOM-Snapshots
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var json = JsonDocument.Parse(line);
            var root = json.RootElement;

            // Parse DOM-Daten
            var timestamp = DateTime.Parse(root.GetProperty("ts_utc").GetString()!);
            var bidPrice = root.GetProperty("bb").GetProperty("p").GetDecimal();
            var bidSize = root.GetProperty("bb").GetProperty("s").GetInt32();
            var askPrice = root.GetProperty("ba").GetProperty("p").GetDecimal();
            var askSize = root.GetProperty("ba").GetProperty("s").GetInt32();

            var snapshot = new DomSnapshot(
                Timestamp: timestamp,
                BidPrice: bidPrice,
                AskPrice: askPrice,
                BidSize: bidSize,
                AskSize: askSize
            );

            processed++;

            // Quality Gate prüfen
            if (!compositeGate.Check(snapshot))
            {
                qualityFailed++;
                continue;
            }
            qualityPassed++;

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

                minZ = Math.Min(minZ, lastZ);
                maxZ = Math.Max(maxZ, lastZ);
            }
        }

        // Output
        _output.WriteLine($"");
        _output.WriteLine($"=== ERGEBNISSE ===");
        _output.WriteLine($"Verarbeitet: {processed}");
        _output.WriteLine($"Quality OK: {qualityPassed}");
        _output.WriteLine($"Quality FAIL: {qualityFailed}");
        _output.WriteLine($"IsWarm: {rollingStats.IsWarm}");
        _output.WriteLine($"Count: {rollingStats.Count}");
        _output.WriteLine($"Letzter Z-Score: {lastZ:F4}");
        _output.WriteLine($"Min Z-Score: {minZ:F4}");
        _output.WriteLine($"Max Z-Score: {maxZ:F4}");
        _output.WriteLine($"Median: {rollingStats.GetMedian():F6}");
        _output.WriteLine($"MAD: {rollingStats.GetMad():F6}");

        // Assert
        processed.Should().Be(718, "Alle Zeilen sollten verarbeitet werden");
        rollingStats.IsWarm.Should().BeTrue("Nach 718 Samples sollte IsWarm=true sein");
        qualityPassed.Should().BeGreaterThan(200, "Genug Samples für Warmup");
        lastZ.Should().BeInRange(-5.0, 5.0, "Z-Score sollte geclippt sein");
    }
}
