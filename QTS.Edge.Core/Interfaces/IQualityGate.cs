namespace QTS.Edge.Core.Interfaces;

/// <summary>
/// Prüft ob ein DOM-Snapshot die Qualitätskriterien erfüllt.
/// </summary>
public interface IQualityGate
{
    bool Check(IDomSnapshot snapshot);
}
