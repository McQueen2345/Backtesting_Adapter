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
/// Vollständiger Integrationstest: Alle Komponenten mit echten DOM-Daten.
/// Testet die komplette Signal-Pipeline inkl. SignalGenerator.
/// </summary>
public class FullPipelineWithSignalsTest
{
    private readonly ITestOutputHelper _output;

    public FullPipelineWithSignalsTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void FullPipeline_WithRealData_GeneratesSignals()
    {
        // Arrange - Alle Komponenten erstellen
        var config = EdgeConfiguration.Default;
        var structImbCalc = new StructImbCalculator();
        var rollingStats = new RollingStatistics(config.WindowSize, config.MinWarmupSamples);
        var zScoreCalc = new ZScoreCalculator(config);
        var signalGen = new SignalGenerator(config);
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
        _output.WriteLine($"=== FULL PIPELINE TEST MIT ECHTEN DATEN ===");
        _output.WriteLine($"Geladene Datenpunkte: {lines.Length}");
        _output.WriteLine($"");

        // Statistiken
        int processed = 0;
        int signalsGenerated = 0;
        int longSignals = 0;
        int shortSignals = 0;
        int flatSignals = 0;
        int signalChanges = 0;
        int lastSignal = 0;

        var signalHistory = new List<(int Index, DateTime Time, double Z, int Signal, string Change)>();

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
            bool qualityOk = compositeGate.Check(snapshot);
            if (!qualityOk) continue;

            // StructImb berechnen
            double structImb = structImbCalc.Calculate(bidSize, askSize);

            // Zur Statistik hinzufügen
            rollingStats.Add(structImb);

            // Z-Score und Signal berechnen
            double zScore = 0;
            int signal = 0;

            if (rollingStats.IsWarm)
            {
                double median = rollingStats.GetMedian();
                double mad = rollingStats.GetMad();
                zScore = zScoreCalc.Calculate(structImb, median, mad);

                // Signal generieren
                signal = signalGen.Generate(
                    zScore: zScore,
                    timestamp: timestamp,
                    isContextWarm: true,
                    isDataStale: false,
                    qualityGatePassed: true
                );

                signalsGenerated++;

                // Zählen
                if (signal == 1) longSignals++;
                else if (signal == -1) shortSignals++;
                else flatSignals++;

                // Signal-Änderung tracken
                if (signal != lastSignal)
                {
                    string changeType = $"{SignalToString(lastSignal)} → {SignalToString(signal)}";
                    signalHistory.Add((processed, timestamp, zScore, signal, changeType));
                    signalChanges++;
                    lastSignal = signal;
                }
            }
        }

        // Output
        _output.WriteLine($"=== VERARBEITUNGS-STATISTIK ===");
        _output.WriteLine($"Verarbeitet: {processed}");
        _output.WriteLine($"Signale generiert: {signalsGenerated}");
        _output.WriteLine($"");

        _output.WriteLine($"=== SIGNAL-VERTEILUNG ===");
        _output.WriteLine($"LONG (+1):  {longSignals} ({100.0 * longSignals / signalsGenerated:F1}%)");
        _output.WriteLine($"SHORT (-1): {shortSignals} ({100.0 * shortSignals / signalsGenerated:F1}%)");
        _output.WriteLine($"FLAT (0):   {flatSignals} ({100.0 * flatSignals / signalsGenerated:F1}%)");
        _output.WriteLine($"");

        _output.WriteLine($"=== SIGNAL-ÄNDERUNGEN ({signalChanges} total) ===");
        foreach (var (idx, time, z, sig, change) in signalHistory)
        {
            _output.WriteLine($"  [{idx:D4}] {time:HH:mm:ss.fff} | Z={z,7:F3} | {change}");
        }
        _output.WriteLine($"");

        _output.WriteLine($"=== ROLLING STATISTICS ===");
        _output.WriteLine($"Count: {rollingStats.Count}");
        _output.WriteLine($"IsWarm: {rollingStats.IsWarm}");
        _output.WriteLine($"Median: {rollingStats.GetMedian():F6}");
        _output.WriteLine($"MAD: {rollingStats.GetMad():F6}");

        // Assert
        processed.Should().Be(718);
        rollingStats.IsWarm.Should().BeTrue();
        signalsGenerated.Should().BeGreaterThan(500, "Nach Warmup sollten viele Signale generiert werden");

        // Die echten Daten hatten Z-Range von ca. ±1.97, also sollten Signale generiert werden
        signalChanges.Should().BeGreaterThan(0, "Es sollte mindestens eine Signal-Änderung geben");
    }

    private static string SignalToString(int signal) => signal switch
    {
        1 => "LONG",
        -1 => "SHORT",
        _ => "FLAT"
    };
}
