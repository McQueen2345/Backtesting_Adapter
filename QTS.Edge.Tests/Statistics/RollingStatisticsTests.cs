using FluentAssertions;
using QTS.Edge.Core.Statistics;
using Xunit;

namespace QTS.Edge.Tests.Statistics;

public class RollingStatisticsTests
{
    [Fact]
    public void IsWarm_BelowThreshold_ReturnsFalse()
    {
        var stats = new RollingStatistics(36000, 200);
        for (int i = 0; i < 199; i++)
        {
            stats.Add(i);
        }
        stats.IsWarm.Should().BeFalse();
    }

    [Fact]
    public void IsWarm_AtThreshold_ReturnsTrue()
    {
        var stats = new RollingStatistics(36000, 200);
        for (int i = 0; i < 200; i++)
        {
            stats.Add(i);
        }
        stats.IsWarm.Should().BeTrue();
    }

    [Fact]
    public void GetMedian_OddCount_ReturnsMiddle()
    {
        var stats = new RollingStatistics(100, 1);
        stats.Add(1.0);
        stats.Add(2.0);
        stats.Add(3.0);
        stats.Add(4.0);
        stats.Add(5.0); // Sortiert: [1,2,3,4,5] → Median = 3
        stats.GetMedian().Should().Be(3.0);
    }

    [Fact]
    public void GetMedian_EvenCount_ReturnsLower()
    {
        var stats = new RollingStatistics(100, 1);
        stats.Add(1.0);
        stats.Add(2.0);
        stats.Add(3.0);
        stats.Add(4.0); // Sortiert: [1,2,3,4] → Lower Median = 2 (NICHT 2.5!)
        stats.GetMedian().Should().Be(2.0);
    }

    [Fact]
    public void GetMad_KnownValues_ReturnsCorrect()
    {
        var stats = new RollingStatistics(100, 1);
        // Werte: [1,2,3,4,5] → Median = 3
        // Abweichungen: |1-3|=2, |2-3|=1, |3-3|=0, |4-3|=1, |5-3|=2
        // Sortierte Abweichungen: [0,1,1,2,2] → Lower Median = 1
        stats.Add(1.0);
        stats.Add(2.0);
        stats.Add(3.0);
        stats.Add(4.0);
        stats.Add(5.0);
        stats.GetMad().Should().Be(1.0);
    }

    [Fact]
    public void GetMad_AllSameValues_ReturnsZero()
    {
        var stats = new RollingStatistics(100, 1);
        stats.Add(5.0);
        stats.Add(5.0);
        stats.Add(5.0);
        stats.Add(5.0);
        stats.Add(5.0); // Alle gleich → MAD = 0
        stats.GetMad().Should().Be(0.0);
    }

    // === T034-T036: Weitere Tests ===

    [Fact]
    public void Window_ExceedsSize_RemovesOldest()
    {
        var stats = new RollingStatistics(3, 1); // Nur 3 Werte erlaubt
        stats.Add(1.0);
        stats.Add(2.0);
        stats.Add(3.0);
        stats.Add(4.0); // 1.0 wird entfernt (FIFO)

        stats.Count.Should().Be(3);
        // Verbleibende Werte: [2,3,4] → Median = 3
        stats.GetMedian().Should().Be(3.0);
    }

    [Fact]
    public void Reset_ClearsAllData()
    {
        var stats = new RollingStatistics(100, 10);
        for (int i = 0; i < 50; i++)
        {
            stats.Add(i);
        }

        stats.Reset();

        stats.Count.Should().Be(0);
        stats.IsWarm.Should().BeFalse();
    }

    [Fact]
    public void MedianAndMad_FromSameSortedVector_Deterministic()
    {
        var stats = new RollingStatistics(100, 1);
        stats.Add(5.0);
        stats.Add(1.0);
        stats.Add(3.0);
        stats.Add(2.0);
        stats.Add(4.0);

        // Mehrfach aufrufen - muss konsistent sein (selber sortierter Vektor)
        var median1 = stats.GetMedian();
        var mad1 = stats.GetMad();
        var median2 = stats.GetMedian();
        var mad2 = stats.GetMad();

        median1.Should().Be(median2);
        mad1.Should().Be(mad2);

        // Konkrete erwartete Werte: [1,2,3,4,5] → Median=3, MAD=1
        median1.Should().Be(3.0);
        mad1.Should().Be(1.0);
    }

    [Fact]
    public void GetMedian_Empty_ReturnsZero()
    {
        var stats = new RollingStatistics(100, 10);
        stats.GetMedian().Should().Be(0.0);
    }

    [Fact]
    public void GetMad_Empty_ReturnsZero()
    {
        var stats = new RollingStatistics(100, 10);
        stats.GetMad().Should().Be(0.0);
    }
}
