using FluentAssertions;
using Moq;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;
using Xunit;

namespace QTS.Edge.Tests.Gates;

public class CompositeQualityGateTests
{
    [Fact]
    public void Check_AllGatesPass_ReturnsTrue()
    {
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.25m,
            BidSize: 100,
            AskSize: 100
        );

        var gate1 = new Mock<IQualityGate>();
        gate1.Setup(g => g.Check(snapshot)).Returns(true);

        var gate2 = new Mock<IQualityGate>();
        gate2.Setup(g => g.Check(snapshot)).Returns(true);

        var composite = new CompositeQualityGate(gate1.Object, gate2.Object);

        composite.Check(snapshot).Should().BeTrue();
    }

    [Fact]
    public void Check_OneGateFails_ReturnsFalse()
    {
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.25m,
            BidSize: 100,
            AskSize: 100
        );

        var gate1 = new Mock<IQualityGate>();
        gate1.Setup(g => g.Check(snapshot)).Returns(true);

        var gate2 = new Mock<IQualityGate>();
        gate2.Setup(g => g.Check(snapshot)).Returns(false);

        var composite = new CompositeQualityGate(gate1.Object, gate2.Object);

        composite.Check(snapshot).Should().BeFalse();
    }

    [Fact]
    public void Check_EmptyGates_ReturnsTrue()
    {
        var snapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow,
            BidPrice: 100.00m,
            AskPrice: 100.25m,
            BidSize: 100,
            AskSize: 100
        );

        var composite = new CompositeQualityGate();

        // Vacuous truth: Leere Liste → true
        composite.Check(snapshot).Should().BeTrue();
    }
}
