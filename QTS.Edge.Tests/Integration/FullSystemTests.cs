using System.Text.Json;
using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Integration;

/// <summary>
/// Vollständiger System-Test über EdgeFactory.
/// </summary>
public class FullSystemTests
{
    private readonly ITestOutputHelper _output;

    public FullSystemTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void FullSystem_ViaFactory_WorksEndToEnd()
    {
        // Arrange - Edge über Factory erstellen
        var edge = EdgeFactory.Create();

        _output.WriteLine("=== FULL SYSTEM TEST VIA FACTORY ===");
        _output.WriteLine("");

        // Act - 300 Snapshots verarbeiten
        int signalChanges = 0;
        int lastSignal = 0;

        for (int i = 0; i < 300; i++)
        {
            // Variiere Bid/Ask um Signale zu erzeugen
            int bidSize = 100 + (int)(50 * Math.Sin(i * 0.1));
            int askSize = 100 - (int)(50 * Math.Sin(i * 0.1));

            var snapshot = new DomSnapshot(
                Timestamp: DateTime.UtcNow.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: Math.Max(1, bidSize),
                AskSize: Math.Max(1, askSize)
            );

            var result = edge.ProcessSnapshot(snapshot);

            if (result.Signal != lastSignal && result.IsContextWarm)
            {
                _output.WriteLine($"[{i:D4}] Signal: {lastSignal} → {result.Signal} | Z={result.StructImbZ:F3}");
                signalChanges++;
                lastSignal = result.Signal;
            }
        }

        _output.WriteLine("");
        _output.WriteLine($"Signal-Änderungen: {signalChanges}");

        // Reset testen
        edge.Reset();
        var afterReset = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        ));

        _output.WriteLine($"Nach Reset - IsContextWarm: {afterReset.IsContextWarm}");
        _output.WriteLine("");
        _output.WriteLine("✅ Full System Test PASSED!");

        // Assert
        afterReset.IsContextWarm.Should().BeFalse("Nach Reset sollte IsContextWarm=false sein");
        afterReset.Signal.Should().Be(0, "Nach Reset sollte Signal=0 sein");
    }

    [Fact]
    public void FullSystem_WithRealData_ViaFactory()
    {
        // Arrange
        var edge = EdgeFactory.Create();

        // Testdaten laden (Dateiname mit Punkt statt Unterstrich)
        var testDataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "TestData", "ES_YOSHI911_20251110_212851.dom.jsonl"
        );

        var lines = File.ReadAllLines(testDataPath);

        _output.WriteLine("=== FULL SYSTEM TEST MIT ECHTEN DATEN (VIA FACTORY) ===");
        _output.WriteLine($"Datenpunkte: {lines.Length}");

        // Act
        int processed = 0;
        int signalChanges = 0;
        int lastSignal = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var json = JsonDocument.Parse(line);
            var root = json.RootElement;

            var snapshot = new DomSnapshot(
                Timestamp: DateTime.Parse(root.GetProperty("ts_utc").GetString()!),
                BidPrice: root.GetProperty("bb").GetProperty("p").GetDecimal(),
                AskPrice: root.GetProperty("ba").GetProperty("p").GetDecimal(),
                BidSize: root.GetProperty("bb").GetProperty("s").GetInt32(),
                AskSize: root.GetProperty("ba").GetProperty("s").GetInt32()
            );

            var result = edge.ProcessSnapshot(snapshot);
            processed++;

            if (result.Signal != lastSignal && result.IsContextWarm)
            {
                signalChanges++;
                lastSignal = result.Signal;
            }
        }

        _output.WriteLine($"Verarbeitet: {processed}");
        _output.WriteLine($"Signal-Änderungen: {signalChanges}");
        _output.WriteLine("");
        _output.WriteLine("✅ Real Data Test via Factory PASSED!");

        // Assert
        processed.Should().Be(718);
        signalChanges.Should().Be(14, "Sollte gleiche Anzahl Signal-Änderungen wie direkter Test haben");
    }
}
