using QTS.Edge.Core.Interfaces;

namespace QTS.Edge.Core.Gates;

/// <summary>
/// Kombiniert mehrere Quality Gates zu einem.
/// Gibt true zurück nur wenn ALLE Gates true zurückgeben.
/// </summary>
public sealed class CompositeQualityGate : IQualityGate
{
    private readonly IQualityGate[] _gates;

    public CompositeQualityGate(params IQualityGate[] gates)
    {
        _gates = gates;
    }

    /// <inheritdoc />
    public bool Check(IDomSnapshot snapshot)
    {
        // Alle Gates müssen true zurückgeben
        // Leere Liste → true (vacuous truth)
        foreach (var gate in _gates)
        {
            if (!gate.Check(snapshot))
            {
                return false;
            }
        }

        return true;
    }
}
