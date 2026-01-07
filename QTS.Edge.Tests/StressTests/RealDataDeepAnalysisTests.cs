using System.Text.Json;
using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;
using QTS.Edge.Core.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.StressTests;

/// <summary>
/// Tiefgehende Analyse der echten DOM-Daten.
/// Untersucht Muster, Anomalien und potenzielle Probleme.
/// </summary>
public class RealDataDeepAnalysisTests
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDataPath;

    public RealDataDeepAnalysisTests(ITestOutputHelper output)
    {
        _output = output;
        _testDataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "TestData", "ES_YOSHI911_20251110_212851.dom.jsonl"
        );
    }

    private List<DomSnapshot> LoadTestData()
    {
        var snapshots = new List<DomSnapshot>();
        var lines = File.ReadAllLines(_testDataPath);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var json = JsonDocument.Parse(line);
            var root = json.RootElement;

            snapshots.Add(new DomSnapshot(
                Timestamp: DateTime.Parse(root.GetProperty("ts_utc").GetString()!),
                BidPrice: root.GetProperty("bb").GetProperty("p").GetDecimal(),
                AskPrice: root.GetProperty("ba").GetProperty("p").GetDecimal(),
                BidSize: root.GetProperty("bb").GetProperty("s").GetInt32(),
                AskSize: root.GetProperty("ba").GetProperty("s").GetInt32()
            ));
        }

        return snapshots;
    }

    // ============================================================
    // SECTION 1: DATA QUALITY ANALYSIS
    // ============================================================

    [Fact]
    public void RealData_SpreadDistribution_Analysis()
    {
        var snapshots = LoadTestData();
        var config = EdgeConfiguration.Default;

        var spreads = snapshots
            .Select(s => (s.AskPrice - s.BidPrice) / config.TickSize)
            .ToList();

        var distribution = spreads
            .GroupBy(s => (int)s)
            .OrderBy(g => g.Key)
            .Select(g => (Ticks: g.Key, Count: g.Count(), Pct: 100.0 * g.Count() / spreads.Count))
            .ToList();

        _output.WriteLine("=== SPREAD DISTRIBUTION ===");
        foreach (var (ticks, count, pct) in distribution)
        {
            _output.WriteLine($"  {ticks} Tick(s): {count} ({pct:F1}%)");
        }

        var avgSpread = spreads.Average();
        var maxSpread = spreads.Max();
        var minSpread = spreads.Min();

        _output.WriteLine($"\nAverage Spread: {avgSpread:F2} Ticks");
        _output.WriteLine($"Min Spread: {minSpread} Ticks");
        _output.WriteLine($"Max Spread: {maxSpread} Ticks");

        // Alle Spreads sollten valide sein
        spreads.All(s => s >= 0).Should().BeTrue("All spreads should be non-negative");
    }

    [Fact]
    public void RealData_DepthDistribution_Analysis()
    {
        var snapshots = LoadTestData();

        var bidSizes = snapshots.Select(s => s.BidSize).ToList();
        var askSizes = snapshots.Select(s => s.AskSize).ToList();

        _output.WriteLine("=== DEPTH DISTRIBUTION ===");
        _output.WriteLine($"Bid Size - Min: {bidSizes.Min()}, Max: {bidSizes.Max()}, Avg: {bidSizes.Average():F1}");
        _output.WriteLine($"Ask Size - Min: {askSizes.Min()}, Max: {askSizes.Max()}, Avg: {askSizes.Average():F1}");

        // Perzentile
        var sortedBid = bidSizes.OrderBy(x => x).ToList();
        var sortedAsk = askSizes.OrderBy(x => x).ToList();

        _output.WriteLine($"\nBid Size Percentiles:");
        _output.WriteLine($"  P10: {sortedBid[(int)(sortedBid.Count * 0.1)]}");
        _output.WriteLine($"  P50: {sortedBid[(int)(sortedBid.Count * 0.5)]}");
        _output.WriteLine($"  P90: {sortedBid[(int)(sortedBid.Count * 0.9)]}");
        _output.WriteLine($"  P99: {sortedBid[(int)(sortedBid.Count * 0.99)]}");

        _output.WriteLine($"\nAsk Size Percentiles:");
        _output.WriteLine($"  P10: {sortedAsk[(int)(sortedAsk.Count * 0.1)]}");
        _output.WriteLine($"  P50: {sortedAsk[(int)(sortedAsk.Count * 0.5)]}");
        _output.WriteLine($"  P90: {sortedAsk[(int)(sortedAsk.Count * 0.9)]}");
        _output.WriteLine($"  P99: {sortedAsk[(int)(sortedAsk.Count * 0.99)]}");

        // Keine Nullen sollten vorkommen
        bidSizes.All(s => s > 0).Should().BeTrue("All bid sizes should be > 0");
        askSizes.All(s => s > 0).Should().BeTrue("All ask sizes should be > 0");
    }

    [Fact]
    public void RealData_TimestampGaps_Analysis()
    {
        var snapshots = LoadTestData();

        var gaps = new List<double>();
        for (int i = 1; i < snapshots.Count; i++)
        {
            var gap = (snapshots[i].Timestamp - snapshots[i - 1].Timestamp).TotalMilliseconds;
            gaps.Add(gap);
        }

        _output.WriteLine("=== TIMESTAMP GAP ANALYSIS ===");
        _output.WriteLine($"Total Snapshots: {snapshots.Count}");
        _output.WriteLine($"Time Range: {snapshots.First().Timestamp} to {snapshots.Last().Timestamp}");
        _output.WriteLine($"Total Duration: {(snapshots.Last().Timestamp - snapshots.First().Timestamp).TotalSeconds:F1}s");

        _output.WriteLine($"\nGap Statistics:");
        _output.WriteLine($"  Min Gap: {gaps.Min():F1}ms");
        _output.WriteLine($"  Max Gap: {gaps.Max():F1}ms");
        _output.WriteLine($"  Avg Gap: {gaps.Average():F1}ms");

        // Große Lücken finden
        var largeGaps = gaps.Select((g, i) => (Gap: g, Index: i))
            .Where(x => x.Gap > 1000)
            .ToList();

        if (largeGaps.Any())
        {
            _output.WriteLine($"\nLarge Gaps (>1s): {largeGaps.Count}");
            foreach (var (gap, idx) in largeGaps.Take(5))
            {
                _output.WriteLine($"  At index {idx}: {gap:F0}ms");
            }
        }

        // Zeitstempel sollten monoton steigend sein
        bool monotonic = true;
        for (int i = 1; i < snapshots.Count; i++)
        {
            if (snapshots[i].Timestamp < snapshots[i - 1].Timestamp)
            {
                monotonic = false;
                _output.WriteLine($"WARNING: Non-monotonic at {i}");
            }
        }
        monotonic.Should().BeTrue("Timestamps should be monotonically increasing");
    }

    // ============================================================
    // SECTION 2: STRUCTIMB ANALYSIS
    // ============================================================

    [Fact]
    public void RealData_StructImbDistribution_Analysis()
    {
        var snapshots = LoadTestData();
        var calc = new StructImbCalculator();

        var structImbs = snapshots
            .Select(s => calc.Calculate(s.BidSize, s.AskSize))
            .ToList();

        _output.WriteLine("=== STRUCTIMB DISTRIBUTION ===");
        _output.WriteLine($"Min: {structImbs.Min():F4}");
        _output.WriteLine($"Max: {structImbs.Max():F4}");
        _output.WriteLine($"Mean: {structImbs.Average():F4}");

        // Histogram
        var buckets = new Dictionary<string, int>
        {
            { "[-1.0, -0.8)", 0 },
            { "[-0.8, -0.6)", 0 },
            { "[-0.6, -0.4)", 0 },
            { "[-0.4, -0.2)", 0 },
            { "[-0.2, 0.0)", 0 },
            { "[0.0, 0.2)", 0 },
            { "[0.2, 0.4)", 0 },
            { "[0.4, 0.6)", 0 },
            { "[0.6, 0.8)", 0 },
            { "[0.8, 1.0]", 0 },
        };

        foreach (var s in structImbs)
        {
            if (s < -0.8) buckets["[-1.0, -0.8)"]++;
            else if (s < -0.6) buckets["[-0.8, -0.6)"]++;
            else if (s < -0.4) buckets["[-0.6, -0.4)"]++;
            else if (s < -0.2) buckets["[-0.4, -0.2)"]++;
            else if (s < 0.0) buckets["[-0.2, 0.0)"]++;
            else if (s < 0.2) buckets["[0.0, 0.2)"]++;
            else if (s < 0.4) buckets["[0.2, 0.4)"]++;
            else if (s < 0.6) buckets["[0.4, 0.6)"]++;
            else if (s < 0.8) buckets["[0.6, 0.8)"]++;
            else buckets["[0.8, 1.0]"]++;
        }

        _output.WriteLine("\nHistogram:");
        foreach (var (range, count) in buckets)
        {
            var bar = new string('█', (int)(50.0 * count / structImbs.Count));
            _output.WriteLine($"  {range}: {count,4} {bar}");
        }

        // StructImb sollte immer im Bereich [-1, 1] sein
        structImbs.All(s => s >= -1.0 && s <= 1.0).Should().BeTrue();
    }

    [Fact]
    public void RealData_StructImbAutocorrelation_Analysis()
    {
        var snapshots = LoadTestData();
        var calc = new StructImbCalculator();

        var structImbs = snapshots
            .Select(s => calc.Calculate(s.BidSize, s.AskSize))
            .ToList();

        // Berechne Lag-1 bis Lag-5 Autokorrelation
        _output.WriteLine("=== STRUCTIMB AUTOCORRELATION ===");

        for (int lag = 1; lag <= 5; lag++)
        {
            double corr = CalculateAutocorrelation(structImbs, lag);
            var bar = new string('█', (int)(20 * Math.Abs(corr)));
            _output.WriteLine($"  Lag {lag}: {corr:F4} {bar}");
        }

        // Hohe Autokorrelation bedeutet Trendverhalten
    }

    private double CalculateAutocorrelation(List<double> data, int lag)
    {
        if (lag >= data.Count) return 0;

        double mean = data.Average();
        double variance = data.Sum(x => (x - mean) * (x - mean));

        double covariance = 0;
        for (int i = 0; i < data.Count - lag; i++)
        {
            covariance += (data[i] - mean) * (data[i + lag] - mean);
        }

        return covariance / variance;
    }

    // ============================================================
    // SECTION 3: Z-SCORE ANALYSIS
    // ============================================================

    [Fact]
    public void RealData_ZScoreDistribution_Analysis()
    {
        var snapshots = LoadTestData();
        var edge = EdgeFactory.Create();

        var zScores = new List<double>();

        foreach (var snapshot in snapshots)
        {
            var result = edge.ProcessSnapshot(snapshot);
            if (result.IsContextWarm)
            {
                zScores.Add(result.StructImbZ);
            }
        }

        _output.WriteLine("=== Z-SCORE DISTRIBUTION ===");
        _output.WriteLine($"Total warm samples: {zScores.Count}");
        _output.WriteLine($"Min Z: {zScores.Min():F4}");
        _output.WriteLine($"Max Z: {zScores.Max():F4}");
        _output.WriteLine($"Mean Z: {zScores.Average():F4}");

        // Standardabweichung berechnen
        double mean = zScores.Average();
        double stdDev = Math.Sqrt(zScores.Sum(z => (z - mean) * (z - mean)) / zScores.Count);
        _output.WriteLine($"StdDev Z: {stdDev:F4}");

        // Zähle Überschreitungen der Thresholds
        int aboveEntry = zScores.Count(z => z >= 1.5);
        int belowEntry = zScores.Count(z => z <= -1.5);
        int inDeadZone = zScores.Count(z => z > -1.5 && z < 1.5);

        _output.WriteLine($"\nThreshold Analysis:");
        _output.WriteLine($"  Z >= 1.5 (LONG trigger): {aboveEntry} ({100.0 * aboveEntry / zScores.Count:F1}%)");
        _output.WriteLine($"  Z <= -1.5 (SHORT trigger): {belowEntry} ({100.0 * belowEntry / zScores.Count:F1}%)");
        _output.WriteLine($"  In dead zone: {inDeadZone} ({100.0 * inDeadZone / zScores.Count:F1}%)");

        // Z-Scores sollten geklippt sein
        zScores.All(z => z >= -5.0 && z <= 5.0).Should().BeTrue("All Z-scores should be clipped to [-5, 5]");
    }

    [Fact]
    public void RealData_ZScoreTransitions_Analysis()
    {
        var snapshots = LoadTestData();
        var edge = EdgeFactory.Create();

        var transitions = new Dictionary<string, int>
        {
            { "FLAT→LONG", 0 },
            { "FLAT→SHORT", 0 },
            { "LONG→FLAT", 0 },
            { "LONG→SHORT", 0 },
            { "SHORT→FLAT", 0 },
            { "SHORT→LONG", 0 },
        };

        int lastSignal = 0;
        var signalHistory = new List<(int Index, double Z, int Signal)>();

        for (int i = 0; i < snapshots.Count; i++)
        {
            var result = edge.ProcessSnapshot(snapshots[i]);

            if (result.IsContextWarm && result.Signal != lastSignal)
            {
                string transition = $"{SignalName(lastSignal)}→{SignalName(result.Signal)}";
                if (transitions.ContainsKey(transition))
                {
                    transitions[transition]++;
                }
                signalHistory.Add((i, result.StructImbZ, result.Signal));
                lastSignal = result.Signal;
            }
        }

        _output.WriteLine("=== SIGNAL TRANSITIONS ===");
        foreach (var (trans, count) in transitions.OrderByDescending(x => x.Value))
        {
            _output.WriteLine($"  {trans}: {count}");
        }

        _output.WriteLine($"\n=== SIGNAL CHANGE LOG (first 10) ===");
        foreach (var (idx, z, sig) in signalHistory.Take(10))
        {
            _output.WriteLine($"  [{idx:D4}] Z={z:F3} → {SignalName(sig)}");
        }

        // Keine direkten LONG→SHORT ohne FLAT dazwischen?
        // (außer bei Reversal mit Hysteresis)
        int directReversals = transitions["LONG→SHORT"] + transitions["SHORT→LONG"];
        _output.WriteLine($"\nDirect reversals (LONG↔SHORT): {directReversals}");
    }

    private string SignalName(int signal) => signal switch
    {
        1 => "LONG",
        -1 => "SHORT",
        _ => "FLAT"
    };

    // ============================================================
    // SECTION 4: QUALITY GATE ANALYSIS
    // ============================================================

    [Fact]
    public void RealData_QualityGatePassRate_Analysis()
    {
        var snapshots = LoadTestData();
        var config = EdgeConfiguration.Default;
        var spreadGate = new SpreadQualityGate(config);
        var depthGate = new DepthQualityGate(config);
        var compositeGate = new CompositeQualityGate(spreadGate, depthGate);

        int spreadPass = 0, spreadFail = 0;
        int depthPass = 0, depthFail = 0;
        int compositePass = 0, compositeFail = 0;

        foreach (var snapshot in snapshots)
        {
            if (spreadGate.Check(snapshot)) spreadPass++;
            else spreadFail++;

            if (depthGate.Check(snapshot)) depthPass++;
            else depthFail++;

            if (compositeGate.Check(snapshot)) compositePass++;
            else compositeFail++;
        }

        _output.WriteLine("=== QUALITY GATE ANALYSIS ===");
        _output.WriteLine($"Spread Gate: {spreadPass} pass, {spreadFail} fail ({100.0 * spreadPass / snapshots.Count:F1}% pass)");
        _output.WriteLine($"Depth Gate:  {depthPass} pass, {depthFail} fail ({100.0 * depthPass / snapshots.Count:F1}% pass)");
        _output.WriteLine($"Composite:   {compositePass} pass, {compositeFail} fail ({100.0 * compositePass / snapshots.Count:F1}% pass)");

        // Bei echten Daten sollten die meisten durchkommen
        compositePass.Should().BeGreaterThan(snapshots.Count / 2, "More than 50% should pass quality gates");
    }

    // ============================================================
    // SECTION 5: SIGNAL QUALITY ANALYSIS
    // ============================================================

    [Fact]
    public void RealData_SignalDuration_Analysis()
    {
        var snapshots = LoadTestData();
        var edge = EdgeFactory.Create();

        var signalDurations = new List<(int Signal, int DurationTicks)>();
        int currentSignal = 0;
        int signalStartIdx = 0;

        for (int i = 0; i < snapshots.Count; i++)
        {
            var result = edge.ProcessSnapshot(snapshots[i]);

            if (result.IsContextWarm && result.Signal != currentSignal)
            {
                if (currentSignal != 0)
                {
                    signalDurations.Add((currentSignal, i - signalStartIdx));
                }
                currentSignal = result.Signal;
                signalStartIdx = i;
            }
        }

        _output.WriteLine("=== SIGNAL DURATION ANALYSIS ===");

        var longDurations = signalDurations.Where(x => x.Signal == 1).Select(x => x.DurationTicks).ToList();
        var shortDurations = signalDurations.Where(x => x.Signal == -1).Select(x => x.DurationTicks).ToList();

        if (longDurations.Any())
        {
            _output.WriteLine($"LONG Signals: {longDurations.Count}");
            _output.WriteLine($"  Min Duration: {longDurations.Min()} ticks");
            _output.WriteLine($"  Max Duration: {longDurations.Max()} ticks");
            _output.WriteLine($"  Avg Duration: {longDurations.Average():F1} ticks");
        }

        if (shortDurations.Any())
        {
            _output.WriteLine($"\nSHORT Signals: {shortDurations.Count}");
            _output.WriteLine($"  Min Duration: {shortDurations.Min()} ticks");
            _output.WriteLine($"  Max Duration: {shortDurations.Max()} ticks");
            _output.WriteLine($"  Avg Duration: {shortDurations.Average():F1} ticks");
        }
    }

    [Fact]
    public void RealData_FullSystemValidation()
    {
        var snapshots = LoadTestData();
        var edge = EdgeFactory.Create();

        int totalSnapshots = 0;
        int warmSnapshots = 0;
        int qualityPassSnapshots = 0;
        int longSignals = 0;
        int shortSignals = 0;
        int flatSignals = 0;

        double minZ = double.MaxValue;
        double maxZ = double.MinValue;

        foreach (var snapshot in snapshots)
        {
            totalSnapshots++;
            var result = edge.ProcessSnapshot(snapshot);

            if (result.IsContextWarm)
            {
                warmSnapshots++;
                minZ = Math.Min(minZ, result.StructImbZ);
                maxZ = Math.Max(maxZ, result.StructImbZ);
            }

            if (result.IsQualityGatePassed)
            {
                qualityPassSnapshots++;
            }

            if (result.Signal == 1) longSignals++;
            else if (result.Signal == -1) shortSignals++;
            else flatSignals++;
        }

        _output.WriteLine("=== FULL SYSTEM VALIDATION ===");
        _output.WriteLine($"Total Snapshots: {totalSnapshots}");
        _output.WriteLine($"Warm Snapshots: {warmSnapshots} ({100.0 * warmSnapshots / totalSnapshots:F1}%)");
        _output.WriteLine($"Quality Pass: {qualityPassSnapshots} ({100.0 * qualityPassSnapshots / totalSnapshots:F1}%)");
        _output.WriteLine($"\nSignal Distribution:");
        _output.WriteLine($"  LONG:  {longSignals} ({100.0 * longSignals / totalSnapshots:F1}%)");
        _output.WriteLine($"  SHORT: {shortSignals} ({100.0 * shortSignals / totalSnapshots:F1}%)");
        _output.WriteLine($"  FLAT:  {flatSignals} ({100.0 * flatSignals / totalSnapshots:F1}%)");
        _output.WriteLine($"\nZ-Score Range: [{minZ:F4}, {maxZ:F4}]");

        // Validierungen
        totalSnapshots.Should().Be(718, "Should process all 718 snapshots");
        warmSnapshots.Should().BeGreaterThan(500, "Most snapshots should be warm");
        qualityPassSnapshots.Should().Be(totalSnapshots, "All snapshots should pass quality gates");
    }
}
