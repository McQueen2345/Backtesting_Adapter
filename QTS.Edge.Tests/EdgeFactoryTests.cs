using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;
using Xunit;

namespace QTS.Edge.Tests;

public class EdgeFactoryTests
{
    [Fact]
    public void Create_WithDefaultConfig_ReturnsEdge()
    {
        var edge = EdgeFactory.Create();

        edge.Should().NotBeNull();
        edge.Should().BeAssignableTo<IStructImbL1Edge>();
    }

    [Fact]
    public void Create_WithCustomConfig_UsesConfig()
    {
        var customConfig = new EdgeConfiguration
        {
            MinWarmupSamples = 50 // Kleiner für schnelleren Test
        };

        var edge = EdgeFactory.Create(customConfig);

        // Verarbeite 50 Snapshots - sollte dann warm sein
        for (int i = 0; i < 50; i++)
        {
            var snapshot = new DomSnapshot(
                Timestamp: DateTime.UtcNow.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            );
            var result = edge.ProcessSnapshot(snapshot);

            if (i == 49)
            {
                result.IsContextWarm.Should().BeTrue("Nach 50 Samples sollte IsWarm=true sein mit custom config");
            }
        }
    }

    [Fact]
    public void Create_WithQualityGatesDisabled_SkipsGates()
    {
        var configNoGates = new EdgeConfiguration
        {
            EnableQualityGates = false,
            MinWarmupSamples = 10
        };

        var edge = EdgeFactory.Create(configNoGates);

        // Warmup
        for (int i = 0; i < 10; i++)
        {
            edge.ProcessSnapshot(new DomSnapshot(
                Timestamp: DateTime.UtcNow.AddMilliseconds(i * 100),
                BidPrice: 5000.00m,
                AskPrice: 5000.25m,
                BidSize: 100,
                AskSize: 100
            ));
        }

        // Snapshot mit zu großem Spread (normalerweise würde Gate fehlschlagen)
        var badSpreadSnapshot = new DomSnapshot(
            Timestamp: DateTime.UtcNow.AddSeconds(10),
            BidPrice: 5000.00m,
            AskPrice: 5010.00m, // Riesiger Spread
            BidSize: 100,
            AskSize: 100
        );

        var result = edge.ProcessSnapshot(badSpreadSnapshot);

        // Mit deaktivierten Gates sollte es trotzdem durchgehen
        result.IsQualityGatePassed.Should().BeTrue("Mit EnableQualityGates=false sollte Gate immer true sein");
    }
}
