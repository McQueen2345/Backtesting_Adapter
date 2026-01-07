using System.Diagnostics;
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
/// Umfassende Stress-Tests für das gesamte System.
/// Testet Edge Cases, Grenzwerte, numerische Stabilität und unerwartete Szenarien.
/// </summary>
public class ComprehensiveStressTests
{
    private readonly ITestOutputHelper _output;
    private readonly EdgeConfiguration _config;

    public ComprehensiveStressTests(ITestOutputHelper output)
    {
        _output = output;
        _config = EdgeConfiguration.Default;
    }

    // ============================================================
    // SECTION 1: EXTREME VALUE TESTS
    // ============================================================

    [Fact]
    public void StructImb_MaxIntValues_NoOverflow()
    {
        var calc = new StructImbCalculator();

        // Integer.MaxValue auf beiden Seiten
        var result = calc.Calculate(int.MaxValue, int.MaxValue);

        result.Should().Be(0.0, "Gleiche MaxInt-Werte sollten 0 ergeben");
        _output.WriteLine($"MaxInt/MaxInt: {result}");
    }

    [Fact]
    public void StructImb_MaxIntVsOne_NearPlusOne()
    {
        var calc = new StructImbCalculator();

        var result = calc.Calculate(int.MaxValue, 1);

        result.Should().BeApproximately(1.0, 0.0001, "MaxInt vs 1 sollte nahe +1 sein");
        _output.WriteLine($"MaxInt vs 1: {result:F10}");
    }

    [Fact]
    public void StructImb_OneVsMaxInt_NearMinusOne()
    {
        var calc = new StructImbCalculator();

        var result = calc.Calculate(1, int.MaxValue);

        result.Should().BeApproximately(-1.0, 0.0001, "1 vs MaxInt sollte nahe -1 sein");
        _output.WriteLine($"1 vs MaxInt: {result:F10}");
    }

    [Fact]
    public void ZScore_ExtremeValues_StaysClipped()
    {
        var zCalc = new ZScoreCalculator(_config);

        // Extremer StructImb mit minimalem MAD
        var zScore = zCalc.Calculate(0.999, 0.0, 0.001);

        zScore.Should().Be(5.0, "Extrem hoher Z-Score sollte bei +5 gekappt werden");
        _output.WriteLine($"Extreme positive Z: {zScore}");

        var negZ = zCalc.Calculate(-0.999, 0.0, 0.001);
        negZ.Should().Be(-5.0, "Extrem niedriger Z-Score sollte bei -5 gekappt werden");
        _output.WriteLine($"Extreme negative Z: {negZ}");
    }

    [Fact]
    public void ZScore_MadExactlyEpsilon_HandledCorrectly()
    {
        var zCalc = new ZScoreCalculator(_config);

        // MAD genau bei Epsilon (1e-10)
        var zScore = zCalc.Calculate(0.5, 0.0, 1e-10);

        // Bei MAD = Epsilon sollte Z = 0 sein (da MAD < Epsilon-Check)
        zScore.Should().Be(0.0, "MAD bei Epsilon sollte Z=0 ergeben");
        _output.WriteLine($"Z mit MAD=Epsilon: {zScore}");
    }

    [Fact]
    public void ZScore_MadSlightlyAboveEpsilon_Calculates()
    {
        var zCalc = new ZScoreCalculator(_config);

        // MAD knapp über Epsilon
        double mad = 1e-10 + 1e-15;
        var zScore = zCalc.Calculate(0.5, 0.0, mad);

        // Sollte einen (geklippten) Z-Score berechnen
        _output.WriteLine($"Z mit MAD knapp über Epsilon: {zScore}");
        Math.Abs(zScore).Should().BeLessThanOrEqualTo(5.0);
    }

    // ============================================================
    // SECTION 2: BOUNDARY CONDITION TESTS
    // ============================================================

    [Fact]
    public void SpreadGate_ExactlyAtBoundary_Passes()
    {
        var gate = new SpreadQualityGate(_config);

        // Genau 4 Ticks = 1.00 (TickSize = 0.25)
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5001.00m, // Genau 4 Ticks
            BidSize: 100,
            AskSize: 100
        );

        gate.Check(snapshot).Should().BeTrue("4 Ticks sollte noch passieren");
    }

    [Fact]
    public void SpreadGate_OneTickOverBoundary_Fails()
    {
        var gate = new SpreadQualityGate(_config);

        // 5 Ticks = 1.25 (über Grenze)
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5001.25m, // 5 Ticks
            BidSize: 100,
            AskSize: 100
        );

        gate.Check(snapshot).Should().BeFalse("5 Ticks sollte fehlschlagen");
    }

    [Fact]
    public void DepthGate_ExactlyAtMinimum_Passes()
    {
        var gate = new DepthQualityGate(_config);

        // MinDepthL1 = 1
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 1,
            AskSize: 1
        );

        gate.Check(snapshot).Should().BeTrue("Minimum Depth 1 sollte passieren");
    }

    [Fact]
    public void DepthGate_OneBelowMinimum_Fails()
    {
        var gate = new DepthQualityGate(_config);

        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 0,
            AskSize: 1
        );

        gate.Check(snapshot).Should().BeFalse("BidSize=0 sollte fehlschlagen");
    }

    [Fact]
    public void SignalGenerator_ExactlyAtThreshold_TriggersSignal()
    {
        var gen = new SignalGenerator(_config);

        // Z = 1.5 (genau Threshold)
        var signal = gen.Generate(1.5, DateTime.UtcNow, true, false, true);

        signal.Should().Be(1, "Genau am Threshold sollte LONG triggern");
    }

    [Fact]
    public void SignalGenerator_JustBelowThreshold_NoSignal()
    {
        var gen = new SignalGenerator(_config);

        // Z = 1.4999 (knapp unter Threshold)
        var signal = gen.Generate(1.4999, DateTime.UtcNow, true, false, true);

        signal.Should().Be(0, "Knapp unter Threshold sollte FLAT bleiben");
    }

    [Fact]
    public void RollingStats_ExactlyAtWarmup_BecomesWarm()
    {
        var stats = new RollingStatistics(250, 200);

        for (int i = 0; i < 199; i++)
        {
            stats.Add(0.1);
        }
        stats.IsWarm.Should().BeFalse("Bei 199 sollte noch nicht warm sein");

        stats.Add(0.1);
        stats.IsWarm.Should().BeTrue("Bei 200 sollte warm sein");
    }

    // ============================================================
    // SECTION 3: NUMERICAL STABILITY TESTS
    // ============================================================

    [Fact]
    public void RollingStats_VerySmallValues_MaintainsPrecision()
    {
        var stats = new RollingStatistics(100, 10);

        // Füge sehr kleine Werte hinzu
        for (int i = 0; i < 50; i++)
        {
            stats.Add(1e-15 * i);
        }

        var median = stats.GetMedian();
        var mad = stats.GetMad();

        _output.WriteLine($"Tiny values - Median: {median:E10}, MAD: {mad:E10}");

        // Sollte keine NaN oder Infinity produzieren
        double.IsNaN(median).Should().BeFalse();
        double.IsInfinity(median).Should().BeFalse();
        double.IsNaN(mad).Should().BeFalse();
        double.IsInfinity(mad).Should().BeFalse();
    }

    [Fact]
    public void RollingStats_VeryLargeValues_MaintainsPrecision()
    {
        var stats = new RollingStatistics(100, 10);

        // Füge sehr große Werte hinzu (aber im double-Bereich)
        for (int i = 0; i < 50; i++)
        {
            stats.Add(1e15 + i);
        }

        var median = stats.GetMedian();
        var mad = stats.GetMad();

        _output.WriteLine($"Large values - Median: {median:E10}, MAD: {mad:E10}");

        double.IsNaN(median).Should().BeFalse();
        double.IsInfinity(median).Should().BeFalse();
    }

    [Fact]
    public void RollingStats_AlternatingExtremes_NoOverflow()
    {
        var stats = new RollingStatistics(100, 10);

        // Alterniere zwischen +1 und -1 (StructImb-Extremwerte)
        for (int i = 0; i < 100; i++)
        {
            stats.Add(i % 2 == 0 ? 1.0 : -1.0);
        }

        var median = stats.GetMedian();
        var mad = stats.GetMad();

        _output.WriteLine($"Alternating extremes - Median: {median}, MAD: {mad}");

        // Bei alternierenden -1, +1 sollte Median nahe 0 sein
        Math.Abs(median).Should().BeLessThan(0.1);
        // MAD sollte etwa 1 sein
        mad.Should().BeApproximately(1.0, 0.1);
    }

    [Fact]
    public void FullPipeline_IdenticalSnapshots_NoAccumulationError()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // 1000 identische Snapshots
        IEdgeSignal? lastResult = null;
        for (int i = 0; i < 1000; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            );
            lastResult = edge.ProcessSnapshot(snapshot);
        }

        _output.WriteLine($"After 1000 identical: Z={lastResult!.StructImbZ}, Signal={lastResult.Signal}");

        // Z-Score sollte stabil bei 0 sein (keine Drift)
        lastResult.StructImbZ.Should().BeApproximately(0.0, 0.01,
            "Identische Snapshots sollten stabilen Z-Score von 0 ergeben");
    }

    // ============================================================
    // SECTION 4: STATE MACHINE TESTS
    // ============================================================

    [Fact]
    public void SignalGenerator_RapidStateChanges_MaintainsConsistency()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        var stateHistory = new List<(double Z, int Signal)>();

        // Simuliere schnelle Z-Score-Änderungen
        double[] zSequence = { 0, 1.5, 2.0, 1.0, 0.5, -1.5, -2.5, -0.5, 0, 1.8 };

        for (int i = 0; i < zSequence.Length; i++)
        {
            // Genug Zeit zwischen Signalen für Cooldown
            var signal = gen.Generate(zSequence[i], baseTime.AddSeconds(i * 2), true, false, true);
            stateHistory.Add((zSequence[i], signal));
            _output.WriteLine($"Z={zSequence[i]:F1} → Signal={signal}");
        }

        // Überprüfe Konsistenz: Signal sollte nie "springen"
        // (z.B. von LONG direkt zu SHORT ohne FLAT bei niedrigem Z)
        for (int i = 1; i < stateHistory.Count; i++)
        {
            var prev = stateHistory[i - 1];
            var curr = stateHistory[i];

            // Wenn von LONG zu SHORT, muss Z unter -2.25 (Reversal) sein
            if (prev.Signal == 1 && curr.Signal == -1)
            {
                curr.Z.Should().BeLessThanOrEqualTo(-2.25,
                    $"LONG→SHORT erfordert Z <= -2.25, war {curr.Z}");
            }
            // Wenn von SHORT zu LONG, muss Z über +2.25 (Reversal) sein
            if (prev.Signal == -1 && curr.Signal == 1)
            {
                curr.Z.Should().BeGreaterThanOrEqualTo(2.25,
                    $"SHORT→LONG erfordert Z >= 2.25, war {curr.Z}");
            }
        }
    }

    [Fact]
    public void SignalGenerator_CooldownExact_BlocksOnTime()
    {
        var gen = new SignalGenerator(_config);
        var baseTime = DateTime.UtcNow;

        // LONG eingehen
        gen.Generate(1.5, baseTime, true, false, true);

        // Genau bei 1000ms - sollte blockiert sein
        var signalAt1000ms = gen.Generate(-2.5, baseTime.AddMilliseconds(1000), true, false, true);
        signalAt1000ms.Should().Be(1, "Bei genau 1000ms sollte Cooldown noch gelten");

        // Bei 1001ms - sollte durchgehen
        gen.Reset();
        gen.Generate(1.5, baseTime, true, false, true);
        var signalAt1001ms = gen.Generate(-2.5, baseTime.AddMilliseconds(1001), true, false, true);
        signalAt1001ms.Should().Be(-1, "Bei 1001ms sollte Cooldown abgelaufen sein");
    }

    // ============================================================
    // SECTION 5: EDGE CASE SCENARIOS
    // ============================================================

    [Fact]
    public void Edge_TimestampJump_HandledCorrectly()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Normaler Start
        for (int i = 0; i < 200; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));
        }

        // Plötzlich 1 Stunde Zeitsprung
        var futureSnapshot = new DomSnapshot(
            Timestamp: baseTime.AddHours(1),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        );

        var result = edge.ProcessSnapshot(futureSnapshot);

        // System sollte weiterhin funktionieren
        result.Should().NotBeNull();
        result.IsContextWarm.Should().BeTrue();
        _output.WriteLine($"After 1h jump: Z={result.StructImbZ}, Signal={result.Signal}");
    }

    [Fact]
    public void Edge_TimestampBackwards_HandledGracefully()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Normaler Start
        for (int i = 0; i < 200; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));
        }

        // Zeitstempel in der Vergangenheit (sollte nicht crashen)
        var pastSnapshot = new DomSnapshot(
            Timestamp: baseTime.AddMilliseconds(-1000),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 100,
            AskSize: 100
        );

        var result = edge.ProcessSnapshot(pastSnapshot);

        // System sollte nicht crashen, aber Cooldown könnte betroffen sein
        result.Should().NotBeNull();
        _output.WriteLine($"After backwards timestamp: Signal={result.Signal}");
    }

    [Fact]
    public void Edge_ZeroSpread_StillWorks()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Zero Spread (Bid = Ask) - ungewöhnlich aber möglich
        for (int i = 0; i < 250; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.00m, // Zero Spread
                BidSize: 100 + i,
                AskSize: 100
            );
            edge.ProcessSnapshot(snapshot);
        }

        // Sollte nicht crashen
        _output.WriteLine("Zero spread test completed without crash");
    }

    [Fact]
    public void Edge_NegativeSpread_HandledGracefully()
    {
        var edge = EdgeFactory.Create();

        // Negativer Spread (Bid > Ask) - sollte eigentlich nicht vorkommen
        var weirdSnapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5001.00m,
            AskPrice: 5000.00m, // Bid > Ask!
            BidSize: 100,
            AskSize: 100
        );

        var result = edge.ProcessSnapshot(weirdSnapshot);

        // Spread-Gate sollte dies ablehnen oder System stabil bleiben
        result.Should().NotBeNull();
        _output.WriteLine($"Negative spread: GatePassed={result.IsQualityGatePassed}");
    }

    // ============================================================
    // SECTION 6: PERFORMANCE & LOAD TESTS
    // ============================================================

    [Fact]
    public void Performance_10000Snapshots_UnderOneSecond()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;
        var random = new Random(42);

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 10000; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: random.Next(50, 200),
                AskSize: random.Next(50, 200)
            );
            edge.ProcessSnapshot(snapshot);
        }

        sw.Stop();

        _output.WriteLine($"10000 snapshots processed in {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {sw.ElapsedMilliseconds / 10000.0:F3}ms per snapshot");

        sw.ElapsedMilliseconds.Should().BeLessThan(1000, "10000 Snapshots sollten unter 1s verarbeitet werden");
    }

    [Fact]
    public void Performance_RollingWindow_MemoryStable()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;
        var random = new Random(42);

        // Viele Snapshots um sicherzustellen, dass Window nicht unbegrenzt wächst
        long memoryBefore = GC.GetTotalMemory(true);

        for (int i = 0; i < 50000; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: random.Next(50, 200),
                AskSize: random.Next(50, 200)
            );
            edge.ProcessSnapshot(snapshot);
        }

        long memoryAfter = GC.GetTotalMemory(true);
        long memoryUsed = memoryAfter - memoryBefore;

        _output.WriteLine($"Memory used after 50000 snapshots: {memoryUsed / 1024}KB");

        // Rolling Window sollte konstanten Speicher nutzen
        // Erwartung: ~250 doubles * 8 bytes = ~2KB + Overhead
        memoryUsed.Should().BeLessThan(1024 * 1024, "Memory sollte unter 1MB bleiben");
    }

    // ============================================================
    // SECTION 7: RESET & RECOVERY TESTS
    // ============================================================

    [Fact]
    public void Reset_MultipleResets_StaysConsistent()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        for (int cycle = 0; cycle < 5; cycle++)
        {
            // Warmup
            for (int i = 0; i < 200; i++)
            {
                edge.ProcessSnapshot(new DomSnapshot(
                    Timestamp: baseTime.AddMilliseconds(i * 100),
                    BidPrice: 5000.00m,
                    AskPrice: 5000.25m,
                    BidSize: 100,
                    AskSize: 100
                ));
            }

            var warmResult = edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(200 * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));

            warmResult.IsContextWarm.Should().BeTrue($"Cycle {cycle}: Should be warm after 200 samples");

            // Reset
            edge.Reset();

            var afterReset = edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddSeconds(1000 + cycle * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));

            afterReset.IsContextWarm.Should().BeFalse($"Cycle {cycle}: Should be cold after reset");
            afterReset.Signal.Should().Be(0, $"Cycle {cycle}: Signal should be 0 after reset");
        }

        _output.WriteLine("5 reset cycles completed successfully");
    }

    // ============================================================
    // SECTION 8: DATA INTEGRITY TESTS
    // ============================================================

    [Fact]
    public void Determinism_SameInputs_SameOutputs()
    {
        var results1 = new List<(double Z, int Signal)>();
        var results2 = new List<(double Z, int Signal)>();

        for (int run = 0; run < 2; run++)
        {
            var edge = EdgeFactory.Create();
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var currentResults = run == 0 ? results1 : results2;

            for (int i = 0; i < 300; i++)
            {
                int bidSize = 100 + (int)(50 * Math.Sin(i * 0.1));
                int askSize = 100 - (int)(50 * Math.Sin(i * 0.1));

                var snapshot = new DomSnapshot(
                    Timestamp: baseTime.AddMilliseconds(i * 100),
                    BidPrice: 5000.00m,
                    AskPrice: 5000.25m,
                    BidSize: Math.Max(1, bidSize),
                    AskSize: Math.Max(1, askSize)
                );

                var result = edge.ProcessSnapshot(snapshot);
                currentResults.Add((result.StructImbZ, result.Signal));
            }
        }

        // Beide Läufe sollten identisch sein
        for (int i = 0; i < results1.Count; i++)
        {
            results1[i].Z.Should().BeApproximately(results2[i].Z, 1e-10,
                $"Z-Score at {i} differs between runs");
            results1[i].Signal.Should().Be(results2[i].Signal,
                $"Signal at {i} differs between runs");
        }

        _output.WriteLine("Determinism verified: Both runs produced identical results");
    }

    [Fact]
    public void EdgeSignal_AllFieldsPopulated()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Warmup
        for (int i = 0; i < 200; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100 + i,
                AskSize: 100
            ));
        }

        var result = edge.ProcessSnapshot(new DomSnapshot(
            Timestamp: baseTime.AddMilliseconds(200 * 100),
            BidPrice: 5000.00m,
            AskPrice: 5000.25m,
            BidSize: 200,
            AskSize: 50
        ));

        // Alle Felder prüfen
        _output.WriteLine($"Signal: {result.Signal}");
        _output.WriteLine($"StructImbZ: {result.StructImbZ}");
        _output.WriteLine($"IsContextWarm: {result.IsContextWarm}");
        _output.WriteLine($"IsDataStale: {result.IsDataStale}");
        _output.WriteLine($"IsQualityGatePassed: {result.IsQualityGatePassed}");
        _output.WriteLine($"Timestamp: {result.Timestamp}");

        // Keine NaN oder unerwarteten Werte
        double.IsNaN(result.StructImbZ).Should().BeFalse();
        double.IsInfinity(result.StructImbZ).Should().BeFalse();
        result.Signal.Should().BeInRange(-1, 1);
    }
}
