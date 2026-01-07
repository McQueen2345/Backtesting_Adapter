using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;

namespace QTS.Edge.Core.Gates;

/// <summary>
/// Prüft ob die Markttiefe auf beiden Seiten ausreichend ist.
/// </summary>
public sealed class DepthQualityGate : IQualityGate
{
    private readonly int _minDepthL1;

    public DepthQualityGate(EdgeConfiguration config)
    {
        _minDepthL1 = config.MinDepthL1;
    }

    /// <inheritdoc />
    public bool Check(IDomSnapshot snapshot)
    {
        // Beide Seiten müssen mindestens MinDepthL1 haben
        return snapshot.BidSize >= _minDepthL1 && snapshot.AskSize >= _minDepthL1;
    }
}
