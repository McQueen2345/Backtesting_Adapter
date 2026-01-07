using FluentAssertions;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Models;
using Xunit;

namespace QTS.Edge.Tests.Gates;

public class DepthQualityGateTests
{
    private readonly DepthQualityGate _gate;

    public DepthQualityGateTests()
    {
        _gate = new DepthQualityGate(EdgeConfiguration.Default);
    }

    [Fact]
    public void Check_BothSidesAboveMinimum_ReturnsTrue()
    {
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
    public void Check_BothSidesExactlyMinimum_ReturnsTrue()
    {
        // MinDepthL1 = 1, beide Seiten = 1
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.25m,
            BidSize: 1,
            AskSize: 1
        );

        _gate.Check(snapshot).Should().BeTrue();
    }

    [Fact]
    public void Check_BidSizeZero_ReturnsFalse()
    {
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.25m,
            BidSize: 0,
            AskSize: 100
        );

        _gate.Check(snapshot).Should().BeFalse();
    }

    [Fact]
    public void Check_AskSizeZero_ReturnsFalse()
    {
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.25m,
            BidSize: 100,
            AskSize: 0
        );

        _gate.Check(snapshot).Should().BeFalse();
    }

    [Fact]
    public void Check_BothSidesZero_ReturnsFalse()
    {
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.25m,
            BidSize: 0,
            AskSize: 0
        );

        _gate.Check(snapshot).Should().BeFalse();
    }
}
