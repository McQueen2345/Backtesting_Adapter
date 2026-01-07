namespace QTS.Edge.Core.Interfaces;

/// <summary>
/// Hauptinterface für den StructImb_L1_Strong Edge.
/// Orchestriert alle Komponenten und verarbeitet DOM-Snapshots.
/// </summary>
public interface IStructImbL1Edge
{
    IEdgeSignal ProcessSnapshot(IDomSnapshot snapshot);
    void Reset();
}
