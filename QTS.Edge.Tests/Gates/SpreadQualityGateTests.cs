using FluentAssertions;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Models;
using Xunit;

namespace QTS.Edge.Tests.Gates;

public class SpreadQualityGateTests
{
    private readonly SpreadQualityGate _gate;

    public SpreadQualityGateTests()
    {
        _gate = new SpreadQualityGate(EdgeConfiguration.Default);
    }

    [Fact]
    public void Check_SpreadExact4Ticks_ReturnsTrue()
    {
        // Spread = 4 Ticks = 4 * 0.25 = 1.00
        // BidPrice=100.00, AskPrice=101.00 → Spread = 1.00 / 0.25 = 4 Ticks
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 101.00m,
            BidSize: 100,
            AskSize: 100
        );

        _gate.Check(snapshot).Should().BeTrue();
    }

    [Fact]
    public void Check_SpreadExceeds4Ticks_ReturnsFalse()
    {
        // Spread = 5 Ticks = 5 * 0.25 = 1.25
        // BidPrice=100.00, AskPrice=101.25 → Spread = 1.25 / 0.25 = 5 Ticks
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 101.25m,
            BidSize: 100,
            AskSize: 100
        );

        _gate.Check(snapshot).Should().BeFalse();
    }

    [Fact]
    public void Check_SpreadBelow4Ticks_ReturnsTrue()
    {
        // Spread = 1 Tick = 1 * 0.25 = 0.25
        // BidPrice=100.00, AskPrice=100.25 → Spread = 0.25 / 0.25 = 1 Tick
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.25m,
            BidSize: 100,
            AskSize: 100
        );

        _gate.Check(snapshot).Should().BeTrue();
    }

    [Fact]
    public void Check_ZeroSpread_ReturnsTrue()
    {
        // Spread = 0 Ticks (Bid = Ask, ungewöhnlich aber möglich)
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.00m,
            BidSize: 100,
            AskSize: 100
        );

        _gate.Check(snapshot).Should().BeTrue();
    }

    [Fact]
    public void Check_DecimalPrecision_NoFloatingPointError()
    {
        // Test für Präzision: 4 Ticks exakt, kein 3.9999...
        // BidPrice=5000.00, AskPrice=5001.00 → Spread = 1.00 / 0.25 = 4.0 exakt
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 5000.00m,
            AskPrice: 5001.00m,
            BidSize: 100,
            AskSize: 100
        );

        _gate.Check(snapshot).Should().BeTrue();
    }
}
