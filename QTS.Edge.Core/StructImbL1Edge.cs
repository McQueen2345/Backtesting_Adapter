using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.Core.Models;

namespace QTS.Edge.Core;

/// <summary>
/// Hauptklasse für den StructImb_L1_Strong Edge.
/// Orchestriert alle Komponenten und verarbeitet DOM-Snapshots.
/// </summary>
public sealed class StructImbL1Edge : IStructImbL1Edge
{
    private readonly IStructImbCalculator _structImbCalculator;
    private readonly IRollingStatistics _rollingStatistics;
    private readonly IZScoreCalculator _zScoreCalculator;
    private readonly ISignalGenerator _signalGenerator;
    private readonly IQualityGate _qualityGate;
    private readonly EdgeConfiguration _config;

    public StructImbL1Edge(
        IStructImbCalculator structImbCalculator,
        IRollingStatistics rollingStatistics,
        IZScoreCalculator zScoreCalculator,
        ISignalGenerator signalGenerator,
        IQualityGate qualityGate,
        EdgeConfiguration config)
    {
        _structImbCalculator = structImbCalculator;
        _rollingStatistics = rollingStatistics;
        _zScoreCalculator = zScoreCalculator;
        _signalGenerator = signalGenerator;
        _qualityGate = qualityGate;
        _config = config;
    }

    /// <inheritdoc />
    public IEdgeSignal ProcessSnapshot(IDomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        // Input-Validation: Ungültige Preise sofort ablehnen
        if (snapshot.BidPrice <= 0 || snapshot.AskPrice <= 0)
        {
            return new EdgeSignal(
                snapshot.Timestamp,
                StructImbRaw: 0.0,
                StructImbZ: 0.0,
                Signal: 0,
                IsContextWarm: _rollingStatistics.IsWarm,
                IsDataStale: true,
                IsQualityGatePassed: false
            );
        }

        // 1. Quality Gate prüfen
        bool qualityGatePassed = _qualityGate.Check(snapshot);

        // 2. + 3. WENN QualityGate FAIL oder IsDataStale → Signal=0, KEIN Add()!
        if (!qualityGatePassed || snapshot.IsDataStale)
        {
            return new EdgeSignal(
                Timestamp: snapshot.Timestamp,
                StructImbRaw: 0.0,
                StructImbZ: 0.0,
                Signal: 0,
                IsContextWarm: _rollingStatistics.IsWarm,
                IsDataStale: snapshot.IsDataStale,
                IsQualityGatePassed: qualityGatePassed
            );
        }

        // 4. StructImb berechnen
        double structImbRaw = _structImbCalculator.Calculate(snapshot.BidSize, snapshot.AskSize);

        // 5. RollingStats.Add() (NUR wenn !stale && qualityOk - hier garantiert)
        _rollingStatistics.Add(structImbRaw);

        // 6. Wenn IsWarm: Z-Score berechnen, sonst Z=0
        double structImbZ = 0.0;
        if (_rollingStatistics.IsWarm)
        {
            double median = _rollingStatistics.GetMedian();
            double mad = _rollingStatistics.GetMad();
            structImbZ = _zScoreCalculator.Calculate(structImbRaw, median, mad);
        }

        // 7. Signal generieren
        int signal = _signalGenerator.Generate(
            zScore: structImbZ,
            timestamp: snapshot.Timestamp,
            isContextWarm: _rollingStatistics.IsWarm,
            isDataStale: snapshot.IsDataStale,
            qualityGatePassed: qualityGatePassed
        );

        // 8. EdgeSignal zurückgeben
        return new EdgeSignal(
            Timestamp: snapshot.Timestamp,
            StructImbRaw: structImbRaw,
            StructImbZ: structImbZ,
            Signal: signal,
            IsContextWarm: _rollingStatistics.IsWarm,
            IsDataStale: snapshot.IsDataStale,
            IsQualityGatePassed: qualityGatePassed
        );
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rollingStatistics.Reset();
        _signalGenerator.Reset();
    }
}
