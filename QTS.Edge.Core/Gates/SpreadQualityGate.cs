using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;

namespace QTS.Edge.Core.Gates;

/// <summary>
/// Prüft ob der Spread innerhalb des erlaubten Bereichs liegt.
/// WICHTIG: Komplett in decimal rechnen für Präzision!
/// </summary>
public sealed class SpreadQualityGate : IQualityGate
{
    private readonly int _maxSpreadTicks;
    private readonly decimal _tickSize;

    public SpreadQualityGate(EdgeConfiguration config)
    {
        _maxSpreadTicks = config.MaxSpreadTicks;
        _tickSize = config.TickSize;
    }

    /// <inheritdoc />
    public bool Check(IDomSnapshot snapshot)
    {
        try
        {
            // Validierung: Preise müssen positiv sein
            if (snapshot.BidPrice <= 0 || snapshot.AskPrice <= 0)
                return false;

            // Invertierter Markt ist ungültig
            if (snapshot.AskPrice < snapshot.BidPrice)
                return false;

            decimal spreadTicks = (snapshot.AskPrice - snapshot.BidPrice)
                                  / _tickSize;
            return spreadTicks <= _maxSpreadTicks;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
