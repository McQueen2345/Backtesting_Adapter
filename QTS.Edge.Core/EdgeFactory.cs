using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Gates;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Statistics;

namespace QTS.Edge.Core;

/// <summary>
/// Factory für die Erstellung von StructImbL1Edge Instanzen.
/// </summary>
public static class EdgeFactory
{
    /// <summary>
    /// Erstellt eine vollständig konfigurierte StructImbL1Edge Instanz.
    /// </summary>
    /// <param name="config">Konfiguration (optional, Default wird verwendet wenn null)</param>
    /// <returns>Konfigurierte IStructImbL1Edge Instanz</returns>
    public static IStructImbL1Edge Create(EdgeConfiguration? config = null)
    {
        config ??= EdgeConfiguration.Default;

        var structImbCalc = new StructImbCalculator();
        var rollingStats = new RollingStatistics(config.WindowSize, config.MinWarmupSamples);
        var zScoreCalc = new ZScoreCalculator(config);
        var signalGen = new SignalGenerator(config);

        var qualityGate = config.EnableQualityGates
            ? new CompositeQualityGate(
                new SpreadQualityGate(config),
                new DepthQualityGate(config))
            : new CompositeQualityGate(); // Leeres Gate = immer true

        return new StructImbL1Edge(
            structImbCalc,
            rollingStats,
            zScoreCalc,
            signalGen,
            qualityGate,
            config
        );
    }
}
