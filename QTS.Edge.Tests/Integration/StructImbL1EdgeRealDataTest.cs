using System.Text.Json;
using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Models;
using QTS.Edge.Core.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Integration;

/// <summary>
/// Test des StructImbL1Edge mit echten ES Futures DOM-Daten.
/// </summary>
public class StructImbL1EdgeRealDataTest
{
    private readonly ITestOutputHelper _output;

    public StructImbL1EdgeRealDataTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void StructImbL1Edge_WithRealDomData_GeneratesValidSignals()
    {
        // Arrange
        var config = EdgeConfiguration.Default;
        var edge = new StructImbL1Edge(
            new StructImbCalculator(),
            new RollingStatistics(config.WindowSize, config.MinWarmupSamples),
            new ZScoreCalculator(config),
            new SignalGenerator(config),
            new CompositeQualityGate(
                new SpreadQualityGate(config),
                new DepthQualityGate(config)
            ),
            config
        );

        // Testdaten laden (Dateiname mit Punkt statt Unterstrich)
        var testDataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "TestData", "ES_YOSHI911_20251110_212851.dom.jsonl"
        );

        var lines = File.ReadAllLines(testDataPath);
        _output.WriteLine($"=== STRUCTIMBL1EDGE TEST MIT ECHTEN DATEN ===");
        _output.WriteLine($"Geladene Datenpunkte: {lines.Length}");
        _output.WriteLine($"");

        // Statistiken
        int processed = 0;
        int longSignals = 0;
        int shortSignals = 0;
        int flatSignals = 0;
        int signalChanges = 0;
        int lastSignal = 0;

        var signalHistory = new List<(int Index, DateTime Time, double Z, int Signal, string Change)>();

        // Act - Verarbeite alle DOM-Snapshots über StructImbL1Edge
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

            // EDGE verarbeitet alles in einem Aufruf!
            var result = edge.ProcessSnapshot(snapshot);

            // Zählen nach Warmup
            if (result.IsContextWarm)
            {
                if (result.Signal == 1) longSignals++;
                else if (result.Signal == -1) shortSignals++;
                else flatSignals++;

                // Signal-Änderung tracken
                if (result.Signal != lastSignal)
                {
                    string changeType = $"{SignalToString(lastSignal)} → {SignalToString(result.Signal)}";
                    signalHistory.Add((processed, timestamp, result.StructImbZ, result.Signal, changeType));
                    signalChanges++;
                    lastSignal = result.Signal;
                }
            }
        }

        // Output
        _output.WriteLine($"=== VERARBEITUNGS-STATISTIK ===");
        _output.WriteLine($"Verarbeitet: {processed}");
        _output.WriteLine($"");

        _output.WriteLine($"=== SIGNAL-VERTEILUNG (nach Warmup) ===");
        int totalAfterWarmup = longSignals + shortSignals + flatSignals;
        _output.WriteLine($"LONG (+1):  {longSignals} ({100.0 * longSignals / totalAfterWarmup:F1}%)");
        _output.WriteLine($"SHORT (-1): {shortSignals} ({100.0 * shortSignals / totalAfterWarmup:F1}%)");
        _output.WriteLine($"FLAT (0):   {flatSignals} ({100.0 * flatSignals / totalAfterWarmup:F1}%)");
        _output.WriteLine($"");

        _output.WriteLine($"=== SIGNAL-ÄNDERUNGEN ({signalChanges} total) ===");
        foreach (var (idx, time, z, sig, change) in signalHistory)
        {
            _output.WriteLine($"  [{idx:D4}] {time:HH:mm:ss.fff} | Z={z,7:F3} | {change}");
        }

        // Assert
        processed.Should().Be(718);
        totalAfterWarmup.Should().BeGreaterThan(500);
        signalChanges.Should().BeGreaterThan(0, "Es sollte mindestens eine Signal-Änderung geben");

        // Signal-Verteilung sollte ähnlich sein wie beim manuellen Test
        _output.WriteLine($"");
        _output.WriteLine($"✅ StructImbL1Edge funktioniert korrekt mit echten Daten!");
    }

    private static string SignalToString(int signal) => signal switch
    {
        1 => "LONG",
        -1 => "SHORT",
        _ => "FLAT"
    };
}
