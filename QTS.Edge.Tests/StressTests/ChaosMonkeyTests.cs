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
/// Chaos Monkey Tests - Unerwartete, kreative und bösartige Szenarien.
/// Testet die Robustheit gegen unvorhergesehene Inputs.
/// </summary>
public class ChaosMonkeyTests
{
    private readonly ITestOutputHelper _output;

    public ChaosMonkeyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ============================================================
    // CHAOS SCENARIO 1: Random Data Attacks
    // ============================================================

    [Fact]
    public void Chaos_RandomBidAskSizes_NeverCrashes()
    {
        var edge = EdgeFactory.Create();
        var random = new Random(12345);
        var baseTime = DateTime.UtcNow;

        int exceptions = 0;
        int processed = 0;

        for (int i = 0; i < 10000; i++)
        {
            try
            {
                var snapshot = new DomSnapshot(
                    Timestamp: baseTime.AddMilliseconds(i * 100),
                    BidPrice: 5000.00m + (decimal)(random.NextDouble() - 0.5) * 10,
                    AskPrice: 5000.25m + (decimal)(random.NextDouble() - 0.5) * 10,
                    BidSize: random.Next(-100, 10000),  // Auch negative Werte!
                    AskSize: random.Next(-100, 10000)
                );
                edge.ProcessSnapshot(snapshot);
                processed++;
            }
            catch (Exception ex)
            {
                exceptions++;
                _output.WriteLine($"Exception at {i}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        _output.WriteLine($"Processed: {processed}, Exceptions: {exceptions}");
        exceptions.Should().Be(0, "No exceptions should occur with random data");
    }

    [Fact]
    public void Chaos_ZeroAndNegativePrices_Handled()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        var weirdPrices = new[]
        {
            (Bid: 0m, Ask: 0m),
            (Bid: -100m, Ask: 100m),
            (Bid: 100m, Ask: -100m),
            (Bid: decimal.MaxValue, Ask: decimal.MinValue),
            (Bid: 0.0001m, Ask: 0.0002m),
        };

        foreach (var (bid, ask) in weirdPrices)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime,
                BidPrice: bid,
                AskPrice: ask,
                BidSize: 100,
                AskSize: 100
            );

            var action = () => edge.ProcessSnapshot(snapshot);
            action.Should().NotThrow($"Weird prices Bid={bid}, Ask={ask} should not crash");
        }

        _output.WriteLine("All weird price combinations handled without crash");
    }

    [Fact]
    public void Chaos_ExtremeDateTimes_Handled()
    {
        var edge = EdgeFactory.Create();

        var extremeDates = new[]
        {
            DateTime.MinValue,
            DateTime.MaxValue,
            new DateTime(1, 1, 1),
            new DateTime(9999, 12, 31, 23, 59, 59),
            DateTime.UnixEpoch,
        };

        foreach (var date in extremeDates)
        {
            var snapshot = new DomSnapshot(
                Timestamp: date,
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            );

            var action = () => edge.ProcessSnapshot(snapshot);
            action.Should().NotThrow($"Extreme date {date} should not crash");
        }

        _output.WriteLine("All extreme dates handled without crash");
    }

    // ============================================================
    // CHAOS SCENARIO 2: Pattern Attacks
    // ============================================================

    [Fact]
    public void Chaos_AllZeros_Handled()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // 500 Snapshots mit BidSize=AskSize=0
        for (int i = 0; i < 500; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 0,
                AskSize: 0
            );

            var result = edge.ProcessSnapshot(snapshot);

            // Quality Gate sollte fehlschlagen
            result.IsQualityGatePassed.Should().BeFalse("Zero depth should fail quality gate");
        }

        _output.WriteLine("All-zeros attack handled correctly");
    }

    [Fact]
    public void Chaos_AlternatingExtremes_NoRunaway()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Schnelles Wechseln zwischen extremen Werten
        for (int i = 0; i < 1000; i++)
        {
            int bidSize = i % 2 == 0 ? 10000 : 1;
            int askSize = i % 2 == 0 ? 1 : 10000;

            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: bidSize,
                AskSize: askSize
            );

            var result = edge.ProcessSnapshot(snapshot);

            // Z-Score sollte immer geklippt sein
            Math.Abs(result.StructImbZ).Should().BeLessThanOrEqualTo(5.0,
                $"Z-Score at {i} should be clipped");
        }

        _output.WriteLine("Alternating extremes test passed");
    }

    [Fact]
    public void Chaos_MonotonicIncrease_NoOverflow()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Kontinuierlich steigende Werte
        for (int i = 0; i < 1000; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: i + 1,
                AskSize: 100
            );

            var result = edge.ProcessSnapshot(snapshot);

            double.IsNaN(result.StructImbZ).Should().BeFalse();
            double.IsInfinity(result.StructImbZ).Should().BeFalse();
        }

        _output.WriteLine("Monotonic increase test passed");
    }

    // ============================================================
    // CHAOS SCENARIO 3: Timing Attacks
    // ============================================================

    [Fact]
    public void Chaos_SameTimestamp_AllSnapshots()
    {
        var edge = EdgeFactory.Create();
        var fixedTime = DateTime.UtcNow;

        // Alle Snapshots mit gleichem Timestamp
        IEdgeSignal? lastResult = null;
        for (int i = 0; i < 300; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: fixedTime,  // Gleicher Timestamp!
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100 + i % 50,
                AskSize: 100
            );

            lastResult = edge.ProcessSnapshot(snapshot);
        }

        // System sollte trotzdem funktionieren
        lastResult.Should().NotBeNull();
        _output.WriteLine($"Same timestamp test: Final Z={lastResult!.StructImbZ}");
    }

    [Fact]
    public void Chaos_ReverseTimeOrder_Processed()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Timestamps in umgekehrter Reihenfolge
        for (int i = 300; i > 0; i--)
        {
            var snapshot = new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            );

            var action = () => edge.ProcessSnapshot(snapshot);
            action.Should().NotThrow("Reverse time order should not crash");
        }

        _output.WriteLine("Reverse time order test passed");
    }

    // ============================================================
    // CHAOS SCENARIO 4: State Machine Attacks
    // ============================================================

    [Fact]
    public void Chaos_RapidResets_NoMemoryLeak()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        long initialMemory = GC.GetTotalMemory(true);

        for (int cycle = 0; cycle < 100; cycle++)
        {
            // Quick warmup
            for (int i = 0; i < 250; i++)
            {
                edge.ProcessSnapshot(new DomSnapshot(
                    Timestamp: baseTime.AddMilliseconds(cycle * 1000 + i),
                    BidPrice: 5000.00m,
                    AskPrice: 5000.25m,
                    BidSize: 100,
                    AskSize: 100
                ));
            }
            edge.Reset();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long finalMemory = GC.GetTotalMemory(true);
        long memoryGrowth = finalMemory - initialMemory;

        _output.WriteLine($"Memory growth after 100 reset cycles: {memoryGrowth / 1024}KB");

        // Sollte kein signifikantes Memory Leak geben
        memoryGrowth.Should().BeLessThan(5 * 1024 * 1024, "Memory growth should be < 5MB");
    }

    [Fact]
    public void Chaos_SignalFlapping_LimitedByHysteresis()
    {
        var config = new EdgeConfiguration { MinWarmupSamples = 10 };
        var gen = new SignalGenerator(config);
        var baseTime = DateTime.UtcNow;

        int signalChanges = 0;
        int lastSignal = 0;

        // Schnelles Flapping zwischen Thresholds
        for (int i = 0; i < 1000; i++)
        {
            // Alterniere zwischen Entry-Threshold (1.5) und Exit-Threshold (0.75)
            double z = i % 2 == 0 ? 1.5 : 0.7;

            int signal = gen.Generate(z, baseTime.AddSeconds(i * 2), true, false, true);

            if (signal != lastSignal)
            {
                signalChanges++;
                lastSignal = signal;
            }
        }

        _output.WriteLine($"Signal changes in flapping scenario: {signalChanges}");

        // Hysteresis sollte exzessives Flapping verhindern
        // Ohne Hysteresis wären es 500+ Änderungen
        signalChanges.Should().BeLessThan(100, "Hysteresis should limit flapping");
    }

    // ============================================================
    // CHAOS SCENARIO 5: Edge Component Isolation
    // ============================================================

    [Fact]
    public void Chaos_StructImbCalc_EveryIntCombination()
    {
        var calc = new StructImbCalculator();

        // Teste kritische Kombinationen
        var testCases = new[]
        {
            (0, 0),
            (0, 1),
            (1, 0),
            (1, 1),
            (-1, 1),
            (1, -1),
            (-1, -1),
            (int.MaxValue, 0),
            (0, int.MaxValue),
            (int.MaxValue, int.MaxValue),
            (int.MaxValue, 1),
            (1, int.MaxValue),
            (int.MaxValue / 2, int.MaxValue / 2),
        };

        foreach (var (bid, ask) in testCases)
        {
            var result = calc.Calculate(bid, ask);

            double.IsNaN(result).Should().BeFalse($"NaN for ({bid}, {ask})");
            double.IsInfinity(result).Should().BeFalse($"Infinity for ({bid}, {ask})");
            result.Should().BeInRange(-1.0, 1.0, $"Result out of range for ({bid}, {ask})");

            _output.WriteLine($"StructImb({bid}, {ask}) = {result:F6}");
        }
    }

    [Fact]
    public void Chaos_RollingStats_SingleValue_Repeated()
    {
        var stats = new RollingStatistics(100, 10);

        // Nur ein einziger Wert, aber oft wiederholt
        for (int i = 0; i < 200; i++)
        {
            stats.Add(0.42);
        }

        var median = stats.GetMedian();
        var mad = stats.GetMad();

        median.Should().BeApproximately(0.42, 1e-10, "Median of constant values");
        mad.Should().BeApproximately(0.0, 1e-10, "MAD of constant values should be 0");

        _output.WriteLine($"Constant value test: Median={median}, MAD={mad}");
    }

    [Fact]
    public void Chaos_RollingStats_NearZeroValues()
    {
        var stats = new RollingStatistics(100, 10);

        // Werte sehr nahe bei 0
        for (int i = 0; i < 100; i++)
        {
            stats.Add(1e-15 * (i - 50));
        }

        var median = stats.GetMedian();
        var mad = stats.GetMad();

        double.IsNaN(median).Should().BeFalse();
        double.IsNaN(mad).Should().BeFalse();

        _output.WriteLine($"Near-zero test: Median={median:E10}, MAD={mad:E10}");
    }

    // ============================================================
    // CHAOS SCENARIO 6: Real-World Anomalies
    // ============================================================

    [Fact]
    public void Chaos_FlashCrash_Simulation()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Normal phase
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

        // Flash crash: Plötzlich extreme Imbalance
        var flashCrashResults = new List<IEdgeSignal>();
        for (int i = 0; i < 20; i++)
        {
            var result = edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds((200 + i) * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 1,      // Fast keine Käufer
                AskSize: 10000   // Massive Verkäufer
            ));
            flashCrashResults.Add(result);
        }

        _output.WriteLine("Flash crash simulation:");
        foreach (var r in flashCrashResults.TakeLast(5))
        {
            _output.WriteLine($"  Z={r.StructImbZ:F3}, Signal={r.Signal}");
        }

        // System sollte auf Crash reagieren
        flashCrashResults.Last().StructImbZ.Should().BeLessThan(-1.0,
            "Flash crash should produce negative Z-Score");
    }

    [Fact]
    public void Chaos_MarketClose_EmptyBook()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Normaler Handel
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

        // Market Close: Leeres Order Book
        for (int i = 0; i < 50; i++)
        {
            var result = edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds((200 + i) * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 0,
                AskSize: 0
            ));

            result.IsQualityGatePassed.Should().BeFalse("Empty book should fail gate");
            result.Signal.Should().Be(0, "Empty book should produce FLAT signal");
        }

        _output.WriteLine("Market close (empty book) handled correctly");
    }

    [Fact]
    public void Chaos_WideningSpreads_GradualDegradation()
    {
        var edge = EdgeFactory.Create();
        var baseTime = DateTime.UtcNow;

        // Warmup mit normalem Spread
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

        // Spreads werden langsam größer
        int gatePassCount = 0;
        int gateFailCount = 0;

        for (int i = 0; i < 50; i++)
        {
            decimal spread = 0.25m + (i * 0.25m); // Von 1 Tick bis 13 Ticks

            var result = edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: baseTime.AddMilliseconds((200 + i) * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.00m + spread,
                BidSize: 100,
                AskSize: 100
            ));

            if (result.IsQualityGatePassed) gatePassCount++;
            else gateFailCount++;
        }

        _output.WriteLine($"Widening spreads: {gatePassCount} passed, {gateFailCount} failed");

        // Ab 5 Ticks (1.25) sollte Gate fehlschlagen
        gateFailCount.Should().BeGreaterThan(0, "Some wide spreads should fail gate");
    }
}
