namespace QTS.Edge.Core.Interfaces;

/// <summary>
/// Generiert Trading-Signale basierend auf Z-Score mit Hysterese und Cooldown.
/// </summary>
public interface ISignalGenerator
{
    int CurrentSignal { get; }
    DateTime? SignalTimestamp { get; }
    int Generate(double zScore, DateTime timestamp, bool isContextWarm, bool isDataStale, bool qualityGatePassed);
    void Reset();
}
